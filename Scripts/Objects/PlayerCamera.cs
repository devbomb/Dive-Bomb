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

        public float OrbitDistance
        {
            get => _orbitDistance;
            set
            {
                _orbitDistance = value;
            }
        }

        public float OrbitYawRad
        {
            get => _orbitYawRad;
            set
            {
                _orbitYawRad = value;
            }
        }

        public float OrbitPitchRad
        {
            get => _orbitPitchRad;
            set
            {
                _orbitPitchRad = value;
            }
        }

        private Camera3D _camera => GetNode<Camera3D>("%Camera");
        private RayCast3D _raycast => GetNode<RayCast3D>("%RayCast");

        private float _orbitDistance = 6;
        private float _orbitYawRad;
        private float _orbitPitchRad;

        private float _suggestedYawRad;
        private float _suggestedPitchRad;
        private float _suggestedDistance;

        private readonly StateMachine _stateMachine = new StateMachine(typeof(PlayerCameraState));

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

        private partial class PlayerCameraState : State
        {
            protected PlayerCamera _self => _stateMachine.GetParent<PlayerCamera>();
        }

        private partial class Unlocked : PlayerCameraState
        {
            public const float FollowDistance = 6;
            public const float ZoomSpeed = 4;

            public const float RightStickRotSpeedDeg = 180;

            public const float MinOrbitPitchDeg = -89;
            public const float MaxOrbitPitchDeg = 0;

            private Vector3 _prevPos;

            public override void OnStateEntered()
            {
                _self.ApplyAnglesAndDistance();
                _prevPos = _self.GlobalPosition;
            }

            public override void _Input(InputEvent ev)
            {
                if (_self.DisableInput)
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

                if (_self.DisableInput)
                {
                    _prevPos = _self.GlobalPosition;
                    return;
                }

                if (InputService.RightStick.Length() > 0.01f)
                {
                    OrbitWithRightStick(delta);
                }
                else if (_self.AllowAutoRotate)
                {
                    MaintainDistanceAndAutoRotate(delta);
                }

                ZoomToFollowDistance(delta);

                _prevPos = _self.GlobalPosition;
            }

            private void ClampOrbitAngles()
            {
                _self.OrbitYawRad = Mathf.PosMod(_self.OrbitYawRad, Mathf.DegToRad(360));
                _self.OrbitPitchRad = Mathf.Clamp(
                    _self.OrbitPitchRad,
                    Mathf.DegToRad(MinOrbitPitchDeg),
                    Mathf.DegToRad(MaxOrbitPitchDeg)
                );
            }

            private void OrbitWithRightStick(float delta)
            {
                float rotSpeed = Mathf.DegToRad(RightStickRotSpeedDeg);
                _self.OrbitYawRad += -InputService.RightStick.X * rotSpeed * delta;
                _self.OrbitPitchRad += -InputService.RightStick.Y * rotSpeed * delta;
                ClampOrbitAngles();
            }

            private void MaintainDistanceAndAutoRotate(float delta)
            {
                var targetPos = _self.FollowTarget.GlobalPosition;
                var dir = targetPos.DirectionTo(_prevPos);

                var transform = _self.GlobalTransform;
                transform.Origin = targetPos + (dir * FollowDistance);
                transform = transform.LookingAt(targetPos);

                _self._orbitYawRad = transform.Basis.GetEuler().Y;
                _self._orbitPitchRad = transform.Basis.GetEuler().X;
                ClampOrbitAngles();

                _self.ApplyAnglesAndDistance();
            }

            private void ZoomToFollowDistance(float delta)
            {
                _self.OrbitDistance = Mathf.MoveToward(
                    _self.OrbitDistance,
                    FollowDistance,
                    ZoomSpeed * delta
                );
            }
        }

        private partial class SuggestingAngle : PlayerCameraState
        {
            private const float Duration = 0.5f;

            private float _timer;
            private float _initialPitchRad;
            private float _initialYawRad;
            private float _initialDistance;

            public override void OnStateEntered()
            {
                _timer = 0;
                _initialPitchRad = _self.OrbitPitchRad;
                _initialYawRad = _self.OrbitYawRad;
                _initialDistance = _self.OrbitDistance;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                // Move the camera to the suggested angle
                _timer += (float)deltaD;

                float t = _timer / Duration;
                t = Mathf.Min(1, t);

                _self.OrbitPitchRad = Mathf.LerpAngle(
                    _initialPitchRad,
                    _self._suggestedPitchRad,
                    t
                );

                _self.OrbitYawRad = Mathf.LerpAngle(
                    _initialYawRad,
                    _self._suggestedYawRad,
                    t
                );

                _self.OrbitDistance = Mathf.Lerp(
                    _initialDistance,
                    _self._suggestedDistance,
                    t
                );

                // ...unless the player has their OWN idea for a camera angle.
                if (InputService.RightStick.Length() > 0.01f)
                    ChangeState<Unlocked>();
            }
        }

        private partial class UsingFixedPosition : PlayerCameraState
        {
            private const float TransitionDuration = 1;

            private Transform3D _initialPos;
            private float _timer;

            public override void OnStateEntered()
            {
                _initialPos = _self.GlobalTransform;
                _timer = 0;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;
                if (_timer > TransitionDuration)
                    _timer = TransitionDuration;

                float t = _timer / TransitionDuration;

                _self.GlobalTransform = _initialPos.InterpolateWith(
                    _self._fixedPosition,
                    MathUtils.LerpSinusoidal(0, 1, t)
                );
            }
        }

        private partial class Recentering : PlayerCameraState
        {
            private const float Duration = 0.1f;

            private float _timer;
            private float _initialPitchRad;
            private float _initialYawRad;

            public override void OnStateEntered()
            {
                _timer = 0;
                _initialPitchRad = _self.OrbitPitchRad;
                _initialYawRad = _self.OrbitYawRad;
            }

            public override void _Process(double deltaD)
            {
                _timer += (float)deltaD;

                float t = _timer / Duration;

                _self.OrbitPitchRad = Mathf.LerpAngle(_initialPitchRad, 0, t);
                _self.OrbitYawRad = Mathf.LerpAngle(
                    _initialYawRad,
                    _self.FollowTarget.GlobalRotation.Y,
                    t
                );

                if (_timer > Duration)
                {
                    _self.ForceRecenter();
                    ChangeState<Unlocked>();
                    return;
                }

            }
        }
    }
}

