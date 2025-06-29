using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class PlayerCamera : Node3D
    {
        [Export] public Node3D FollowTarget;

        [Export] public bool AllowAutoRotate { get; set; }
        public bool DisableInput { get; set; }

        public Node3D TimeTrialFairyRescuePos => GetNode<Node3D>("%TimeTrialFairyRescuePos");

        public float OrbitDistance { get; set; } = 6;

        public float OrbitYawRad { get; set; }

        public float OrbitPitchRad { get; set; }

        private Camera3D _camera => GetNode<Camera3D>("%Camera");
        private RayCast3D _raycast => GetNode<RayCast3D>("%RayCast");

        private float _suggestedYawRad;
        private float _suggestedPitchRad;
        private float _suggestedDistance;

        private readonly StateMachine _stateMachine = new StateMachine();

        private float _shakeTimer;
        private float _shakeDuration;
        private Vector2 _shakeMagnitude;
        private FastNoiseLite _shakeNoiseX = new FastNoiseLite();
        private FastNoiseLite _shakeNoiseY = new FastNoiseLite();
        private Random _shakeRNG = new Random(1337);

        private float _lagTimer;
        private float _lagDuration;
        private Transform3D _lagPosition;

        private Transform3D _fixedPosition;

        public override void _Ready()
        {
            AddChild(_stateMachine);
            _stateMachine.ChangeState<Unlocked>();

            _raycast.AddException(GetParent<Player>());
        }

        public void Reset()
        {
            _lagTimer = 0;
            _lagDuration = 0;

            _shakeTimer = 0;
            _shakeDuration = 0;

            OrbitDistance = Unlocked.FollowDistance;
            ForceRecenter();

            _stateMachine.ChangeState<Unlocked>();
        }

        public override void _Process(double deltaD)
        {
            if (_shakeTimer < _shakeDuration)
            {
                _shakeTimer += (float)deltaD;

                _camera.Position = new Vector3(
                    _shakeMagnitude.X * _shakeNoiseX.GetNoise1D(_shakeTimer),
                    _shakeMagnitude.Y * _shakeNoiseY.GetNoise1D(_shakeTimer),
                    0
                );
                _camera.Position *= 1f - (_shakeTimer / _shakeDuration);
            }
            else
            {
                _camera.Position = Vector3.Zero;
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            _lagTimer += (float)deltaD;

            ApplyAnglesAndDistance();
        }

        public void MakeCurrent() => _camera.MakeCurrent();

        public void Shake(float magnitude, float frequency, float duration)
        {
            Shake(
                new Vector2(magnitude, magnitude),
                new Vector2(frequency, frequency),
                duration
            );
        }

        public void Shake(
            Vector2 magnitude,
            Vector2 frequency,
            float duration
        )
        {
             _shakeTimer = 0;
            _shakeDuration = duration;
            _shakeMagnitude = magnitude;

            _shakeNoiseX = new FastNoiseLite();
            _shakeNoiseY = new FastNoiseLite();

            _shakeNoiseX.Frequency = frequency.X;
            _shakeNoiseX.Seed = _shakeRNG.Next();

            _shakeNoiseY.Frequency = frequency.Y;
            _shakeNoiseY.Seed = _shakeRNG.Next();
        }

        public void Lag(float duration)
        {
            _lagTimer = 0;
            _lagDuration = duration;
            _lagPosition = GlobalTransform;
        }

        public void ForceRecenter()
        {
            OrbitPitchRad = 0;
            OrbitYawRad = FollowTarget.GlobalRotation.Y;
            ApplyAnglesAndDistance();
            this.ResetPhysicsInterpolation3D();
        }

        public void SuggestAngle(float yawRad, float pitchRad, float distance)
        {
            _suggestedYawRad = yawRad;
            _suggestedPitchRad = pitchRad;
            _suggestedDistance = distance;
            _stateMachine.ChangeState<SuggestingAngle>();
        }

        public void StopSuggestingAngle()
        {
            _stateMachine.ChangeState<Unlocked>();
        }

        public void FixPosition(Transform3D position)
        {
            _fixedPosition = position;
            _stateMachine.ChangeState<UsingFixedPosition>();
        }

        public void StopFixingPosition()
        {
            // HACK: Reuse the lag code to make it smoothly return to normal
            Lag(1);
            _stateMachine.ChangeState<Unlocked>();
        }

        public void Recenter()
        {
            _stateMachine.ChangeState<Recentering>();
        }

        public void ApplyAnglesAndDistance()
        {
            Vector3 dir = Vector3.Back
                .Rotated(Vector3.Right, OrbitPitchRad)
                .Rotated(Vector3.Up, OrbitYawRad);

            Vector3 offset = dir * OrbitDistance;
            var desiredPosition = Transform3D.Identity
                .Translated(FollowTarget.GlobalPosition + offset)
                .LookingAt(FollowTarget.GlobalPosition);

            _raycast.GlobalPosition = FollowTarget.GlobalPosition;
            _raycast.TargetPosition = desiredPosition.Origin - _raycast.GlobalPosition;
            _raycast.GlobalRotation = Vector3.Zero;
            _raycast.ForceUpdateTransform();
            _raycast.ForceRaycastUpdate();

            if (_raycast.IsColliding())
            {
                desiredPosition.Origin = _raycast.GetCollisionPoint();
            }

            if (_lagTimer < _lagDuration)
            {
                float t = Mathf.Min(1, _lagTimer / _lagDuration);
                GlobalTransform = _lagPosition.InterpolateWith(desiredPosition, t);
            }
            else
            {
                GlobalTransform = desiredPosition;
            }
        }

        private class Unlocked : State<PlayerCamera>
        {
            public const float FollowDistance = 6;
            public const float ZoomSpeed = 4;

            public const float RightStickRotSpeedDeg = 180;

            public const float MinOrbitPitchDeg = -89;
            public const float MaxOrbitPitchDeg = 0;

            private Vector3 _prevPos;

            public override void OnStateEntered()
            {
                Self.ApplyAnglesAndDistance();
                _prevPos = Self.GlobalPosition;
            }

            public override void _Input(InputEvent ev)
            {
                if (Self.DisableInput)
                    return;

                if (InputService.RecenterCameraJustPressed(ev))
                {
                    ChangeState<Recentering>();
                    return;
                }
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                if (Self.DisableInput)
                {
                    _prevPos = Self.GlobalPosition;
                    return;
                }

                if (InputService.RightStick.Length() > 0.01f)
                {
                    OrbitWithRightStick(delta);
                }
                else if (Self.AllowAutoRotate)
                {
                    MaintainDistanceAndAutoRotate(delta);
                }

                ZoomToFollowDistance(delta);

                _prevPos = Self.GlobalPosition;
            }

            private void ClampOrbitAngles()
            {
                Self.OrbitYawRad = Mathf.PosMod(Self.OrbitYawRad, Mathf.DegToRad(360));
                Self.OrbitPitchRad = Mathf.Clamp(
                    Self.OrbitPitchRad,
                    Mathf.DegToRad(MinOrbitPitchDeg),
                    Mathf.DegToRad(MaxOrbitPitchDeg)
                );
            }

            private void OrbitWithRightStick(float delta)
            {
                float rotSpeed = Mathf.DegToRad(RightStickRotSpeedDeg);
                Self.OrbitYawRad += -InputService.RightStick.X * rotSpeed * delta;
                Self.OrbitPitchRad += -InputService.RightStick.Y * rotSpeed * delta;
                ClampOrbitAngles();
            }

            private void MaintainDistanceAndAutoRotate(float delta)
            {
                var targetPos = Self.FollowTarget.GlobalPosition;
                var dir = targetPos.DirectionTo(_prevPos);

                var transform = Self.GlobalTransform;
                transform.Origin = targetPos + (dir * FollowDistance);
                transform = transform.LookingAt(targetPos);

                Self.OrbitYawRad = transform.Basis.GetEuler().Y;
                Self.OrbitPitchRad = transform.Basis.GetEuler().X;
                ClampOrbitAngles();

                Self.ApplyAnglesAndDistance();
            }

            private void ZoomToFollowDistance(float delta)
            {
                Self.OrbitDistance = Mathf.MoveToward(
                    Self.OrbitDistance,
                    FollowDistance,
                    ZoomSpeed * delta
                );
            }
        }

        private class SuggestingAngle : State<PlayerCamera>
        {
            private const float Duration = 0.5f;

            private float _timer;
            private float _initialPitchRad;
            private float _initialYawRad;
            private float _initialDistance;

            public override void OnStateEntered()
            {
                _timer = 0;
                _initialPitchRad = Self.OrbitPitchRad;
                _initialYawRad = Self.OrbitYawRad;
                _initialDistance = Self.OrbitDistance;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                // Move the camera to the suggested angle
                _timer += (float)deltaD;

                float t = _timer / Duration;
                t = Mathf.Min(1, t);

                Self.OrbitPitchRad = Mathf.LerpAngle(
                    _initialPitchRad,
                    Self._suggestedPitchRad,
                    t
                );

                Self.OrbitYawRad = Mathf.LerpAngle(
                    _initialYawRad,
                    Self._suggestedYawRad,
                    t
                );

                Self.OrbitDistance = Mathf.Lerp(
                    _initialDistance,
                    Self._suggestedDistance,
                    t
                );

                // ...unless the player has their OWN idea for a camera angle.
                if (InputService.RightStick.Length() > 0.01f)
                    ChangeState<Unlocked>();
            }
        }

        private class UsingFixedPosition : State<PlayerCamera>
        {
            private const float TransitionDuration = 1;

            private Transform3D _initialPos;
            private float _timer;

            public override void OnStateEntered()
            {
                _initialPos = Self.GlobalTransform;
                _timer = 0;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;
                if (_timer > TransitionDuration)
                    _timer = TransitionDuration;

                float t = _timer / TransitionDuration;

                Self.GlobalTransform = _initialPos.InterpolateWith(
                    Self._fixedPosition,
                    MathUtils.LerpSinusoidal(0, 1, t)
                );
            }
        }

        private class Recentering : State<PlayerCamera>
        {
            private const float Duration = 0.1f;

            private float _timer;
            private float _initialPitchRad;
            private float _initialYawRad;

            public override void OnStateEntered()
            {
                _timer = 0;
                _initialPitchRad = Self.OrbitPitchRad;
                _initialYawRad = Self.OrbitYawRad;
            }

            public override void _Process(double deltaD)
            {
                _timer += (float)deltaD;

                float t = _timer / Duration;

                Self.OrbitPitchRad = Mathf.LerpAngle(_initialPitchRad, 0, t);
                Self.OrbitYawRad = Mathf.LerpAngle(
                    _initialYawRad,
                    Self.FollowTarget.GlobalRotation.Y,
                    t
                );

                if (_timer > Duration)
                {
                    Self.ForceRecenter();
                    ChangeState<Unlocked>();
                    return;
                }

            }
        }
    }
}

