using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class PlayerCamera : Node3D
    {
        [Export] public Node3D FollowTarget;
        [Export] public Player Player;

        [Export] public bool AllowAutoRotate { get; set; }
        public bool DisableInput { get; set; }
        public bool IgnoreObstructions { get; set; }

        public bool IsBeingManhandled => _stateMachine.CurrentState is Manhandled;
        public bool IsSuggestingAngle => _stateMachine.CurrentState is SuggestingAngle;

        public Node3D TimeTrialFairyRescuePos => GetNode<Node3D>("%TimeTrialFairyRescuePos");

        public float OrbitDistance { get; set; } = 6;

        public float OrbitYawRad { get; set; }

        public float OrbitPitchRad { get; set; }

        /// <summary>
        /// The global position that the camera will assume while it's being
        /// manhandled (see <see cref="StartManhandling"/>).
        ///
        /// Modify this instead of <see cref="GlobalTransform"/> to ensure
        /// transitions into and out of the manhandled state remain smooth.
        /// </summary>
        public Transform3D ManhandledPosition;
        private float _manhandledTransitionDuration;

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

        public override void _Ready()
        {
            AddChild(_stateMachine);
            _stateMachine.ChangeState<Following>();

            _raycast.AddException(Player);
        }

        public void Reset()
        {
            _lagTimer = 0;
            _lagDuration = 0;

            _shakeTimer = 0;
            _shakeDuration = 0;

            OrbitDistance = Following.FollowDistance;
            ForceRecenter();

            _stateMachine.ChangeState<Following>();
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

        public void StartFollowing(float transitionDuration = 0)
        {
            // HACK: Reuse the lag code to make it smoothly return to normal
            Lag(transitionDuration);
            _stateMachine.ChangeState<Following>();
        }

        public void StartManhandling(Transform3D position, float transitionDuration = 0)
        {
            ManhandledPosition = position;
            _manhandledTransitionDuration = transitionDuration;
            _stateMachine.ChangeState<Manhandled>();
        }

        public void FixPosition(Transform3D position)
        {
            StartManhandling(position, 1);
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

            // Zoom in if our view of the player is obstructed.
            // ...unless we've been told not to, of course.
            if (!IgnoreObstructions)
            {
                _raycast.GlobalPosition = FollowTarget.GlobalPosition;
                _raycast.TargetPosition = desiredPosition.Origin - _raycast.GlobalPosition;
                _raycast.GlobalRotation = Vector3.Zero;
                _raycast.ForceUpdateTransform();
                _raycast.ForceRaycastUpdate();

                if (_raycast.IsColliding())
                {
                    desiredPosition.Origin = _raycast.GetCollisionPoint();
                }
            }

            // If the lag effect is active, tween between our desired position
            // and the lag position.
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

        private class Following : State<PlayerCamera>
        {
            public const float FollowDistance = 6;
            public const float ZoomSpeed = 4;

            public const float RightStickRotSpeedDeg = 180;

            public const float MinOrbitPitchDeg = -89;
            public const float MaxOrbitPitchDeg = 0;

            private Vector3 _prevPos;
            private Vector2 _accumulatedMouseMotion;

            public override void OnStateEntered()
            {
                Self.ApplyAnglesAndDistance();
                _prevPos = Self.GlobalPosition;
                _accumulatedMouseMotion = Vector2.Zero;
            }

            public override void _Input(InputEvent ev)
            {
                if (Self.DisableInput)
                    return;

                if (ev is InputEventMouseMotion m && !Self.DisableInput)
                {
                    if (m.ButtonMask.HasFlag(MouseButtonMask.Middle))
                        _accumulatedMouseMotion += m.ScreenRelative;
                }

                if (InputService.RecenterCameraJustPressed(ev))
                {
                    ChangeState<Recentering>();
                    return;
                }
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                // Don't let the motion of moving platforms affect auto-rotation;
                // in an empty void, the player shouldn't be able to tell the
                // difference between standing on a moving platform or a
                // stationary one.
                _prevPos += Self.Player.LastPlatformVelocity * delta;
                Self.GlobalPosition += Self.Player.LastPlatformVelocity * delta;

                if (Self.DisableInput)
                {
                    Self.ApplyAnglesAndDistance();
                    _prevPos = Self.GlobalPosition;
                    _accumulatedMouseMotion = Vector2.Zero;
                    return;
                }

                bool orbitted = false;

                if (InputService.RightStick.Length() > 0.01f)
                {
                    OrbitWithRightStick(delta);
                    orbitted = true;
                }

                if (_accumulatedMouseMotion.Length() > 0.0001f)
                {
                    OrbitWithMouse(_accumulatedMouseMotion);
                    _accumulatedMouseMotion = Vector2.Zero;
                    orbitted = true;
                }

                if (!orbitted && Self.AllowAutoRotate)
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
                float yawSpeedRad = -InputService.RightStick.X * rotSpeed;
                float pitchSpeedRad = -InputService.RightStick.Y * rotSpeed;

                if (UserSettings.Instance.InvertCameraX) yawSpeedRad *= -1;
                if (UserSettings.Instance.InvertCameraY) pitchSpeedRad *= -1;

                Self.OrbitYawRad += yawSpeedRad * delta;
                Self.OrbitPitchRad += pitchSpeedRad * delta;

                ClampOrbitAngles();
            }

            private void OrbitWithMouse(Vector2 mouseMotion)
            {
                float radsPerPixel = 0.005f;

                if (UserSettings.Instance.InvertCameraX) mouseMotion.X *= -1;
                if (UserSettings.Instance.InvertCameraY) mouseMotion.Y *= -1;

                Self.OrbitYawRad -= radsPerPixel * mouseMotion.X;
                Self.OrbitPitchRad -= radsPerPixel * mouseMotion.Y;
                ClampOrbitAngles();
            }

            private void MaintainDistanceAndAutoRotate(float delta)
            {
                var targetPos = Self.FollowTarget.GlobalPosition;
                var dir = targetPos.DirectionTo(_prevPos);

                var transform = Self.GlobalTransform;
                transform.Origin = targetPos + (dir * FollowDistance);
                transform = transform.LookingAt(targetPos);

                float oldPitchRad = Self.OrbitPitchRad;

                Self.OrbitYawRad = transform.Basis.GetEuler().Y;
                Self.OrbitPitchRad = transform.Basis.GetEuler().X;
                ClampOrbitAngles();

                // Don't allow the camera to auto-rotate downwards unless the
                // player is actually _moving_ downwards.  This allows the
                // player to run straight at the camera without the it gradually
                // rotating above their head.
                if (Self.Player.LocalVelocity.Y >= 0)
                {
                    Self.OrbitPitchRad = MathUtils.SoftLimitMin(
                        Self.OrbitPitchRad,
                        oldPitchRad,
                        Mathf.DegToRad(-25)
                    );
                }

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
                t = MathUtils.LerpSinusoidal(0, 1, t);

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
                    ChangeState<Following>();
            }
        }

        private class Manhandled : State<PlayerCamera>
        {
            private Transform3D _initialPos;
            private float _timer;

            public override void OnStateEntered()
            {
                _initialPos = Self.GlobalTransform;
                _timer = 0;
                UpdatePosition();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                if (_timer > Self._manhandledTransitionDuration)
                    _timer = Self._manhandledTransitionDuration;

                UpdatePosition();
            }

            private void UpdatePosition()
            {
                // Avoid division by zero
                if (Self._manhandledTransitionDuration <= 0)
                {
                    Self.GlobalTransform = Self.ManhandledPosition;
                    return;
                }

                float t = _timer / Self._manhandledTransitionDuration;

                Self.GlobalTransform = _initialPos.InterpolateWith(
                    Self.ManhandledPosition,
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
                    ChangeState<Following>();
                    return;
                }

            }
        }
    }
}

