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

        public float OrbitDistance
        {
            get => _orbitDistance;
            set
            {
                _orbitDistance = value;
                ApplyAnglesAndDistance();
            }
        }

        public float OrbitYawRad
        {
            get => _orbitYawRad;
            set
            {
                _orbitYawRad = value;
                ApplyAnglesAndDistance();
            }
        }

        public float OrbitPitchRad
        {
            get => _orbitPitchRad;
            set
            {
                _orbitPitchRad = value;
                ApplyAnglesAndDistance();
            }
        }

        private Camera3D _camera => GetNode<Camera3D>("%Camera");

        private float _orbitDistance = 6;
        private float _orbitYawRad;
        private float _orbitPitchRad;

        private float _suggestedYawRad;
        private float _suggestedPitchRad;
        private float _suggestedDistance;

        private readonly StateMachine _stateMachine = new StateMachine(typeof(PlayerCameraState));
        private readonly PhysicsInterpolator3D _interpolator = new PhysicsInterpolator3D();

        private float _shakeTimer;
        private float _shakeDuration;
        private Vector2 _shakeMagnitude;
        private FastNoiseLite _shakeNoiseX = new FastNoiseLite();
        private FastNoiseLite _shakeNoiseY = new FastNoiseLite();
        private Random _shakeRNG = new Random(1337);

        public override void _Ready()
        {
            AddChild(_stateMachine);
            AddChild(_interpolator);
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

        public void ResetPhysicsInterpolation()
        {
            _interpolator.ResetPhysicsInterpolation();
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

        public void ForceRecenter()
        {
            OrbitPitchRad = 0;
            OrbitYawRad = FollowTarget.GlobalRotation.Y;
            ApplyAnglesAndDistance();
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

        public void Recenter()
        {
            _stateMachine.ChangeState<Recentering>();
        }

        private void ApplyAnglesAndDistance()
        {
            Vector3 dir = Vector3.Back
                .Rotated(Vector3.Right, OrbitPitchRad)
                .Rotated(Vector3.Up, OrbitYawRad);

            Vector3 offset = dir * OrbitDistance;
            GlobalPosition = FollowTarget.GlobalPosition + offset;
            LookAt(FollowTarget.GlobalPosition);

            // HACK: ensure it works smoothly with physics interpolation
            if (!Engine.IsInPhysicsFrame())
            {
                ResetPhysicsInterpolation();
            }
        }

        private partial class PlayerCameraState : State
        {
            protected PlayerCamera _self => _stateMachine.GetParent<PlayerCamera>();
        }

        private partial class Unlocked : PlayerCameraState
        {
            public float FollowDistance = 6;
            public float ZoomSpeed = 4;

            public float RightStickRotSpeedDeg = 180;

            public float MinOrbitPitchDeg = -89;
            public float MaxOrbitPitchDeg = 0;

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

            public override void _Process(double deltaD)
            {
                float delta = (float)deltaD;

                if (_self.DisableInput)
                {
                    _self.ApplyAnglesAndDistance();
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
                var pos = _self.GlobalPosition;
                var targetPos = _self.FollowTarget.GlobalPosition;
                var dir = targetPos.DirectionTo(pos);

                _self.GlobalPosition = targetPos + (dir * FollowDistance);
                _self.LookAt(targetPos);

                _self._orbitYawRad = _self.GlobalRotation.Y;
                _self._orbitPitchRad = _self.GlobalRotation.X;
                ClampOrbitAngles();

                _self.ResetPhysicsInterpolation();
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

            public override void _Process(double deltaD)
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

                _self.ApplyAnglesAndDistance();

                // ...unless the player has their OWN idea for a camera angle.
                if (InputService.RightStick.Length() > 0.01f)
                    ChangeState<Unlocked>();
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
                _self.ApplyAnglesAndDistance();

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

