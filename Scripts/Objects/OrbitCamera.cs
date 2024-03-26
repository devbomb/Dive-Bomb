using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class OrbitCamera : Camera3D
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

        private float _orbitDistance = 6;
        private float _orbitYawRad;
        private float _orbitPitchRad;

        private float _suggestedYawRad;
        private float _suggestedPitchRad;
        private float _suggestedDistance;

        private readonly StateMachine _stateMachine = new StateMachine(typeof(OrbitCameraState));
        private readonly PhysicsInterpolator3D _interpolator = new PhysicsInterpolator3D();

        public override void _Ready()
        {
            AddChild(_stateMachine);
            AddChild(_interpolator);
            _stateMachine.ChangeState<Unlocked>();
        }

        public void ResetPhysicsInterpolation()
        {
            _interpolator.ResetPhysicsInterpolation();
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

        private partial class OrbitCameraState : State
        {
            protected OrbitCamera _camera => _stateMachine.GetParent<OrbitCamera>();
        }

        private partial class Unlocked : OrbitCameraState
        {
            public float FollowDistance = 6;
            public float ZoomSpeed = 4;

            public float RightStickRotSpeedDeg = 180;

            public float MinOrbitPitchDeg = -89;
            public float MaxOrbitPitchDeg = 0;

            public override void _Input(InputEvent ev)
            {
                if (_camera.DisableInput)
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

                if (_camera.DisableInput)
                {
                    _camera.ApplyAnglesAndDistance();
                    return;
                }

                if (InputService.RightStick.Length() > 0.01f)
                {
                    OrbitWithRightStick(delta);
                }
                else if (_camera.AllowAutoRotate)
                {
                    MaintainDistanceAndAutoRotate(delta);
                }

                ZoomToFollowDistance(delta);
            }

            private void ClampOrbitAngles()
            {
                _camera.OrbitYawRad = Mathf.PosMod(_camera.OrbitYawRad, Mathf.DegToRad(360));
                _camera.OrbitPitchRad = Mathf.Clamp(
                    _camera.OrbitPitchRad,
                    Mathf.DegToRad(MinOrbitPitchDeg),
                    Mathf.DegToRad(MaxOrbitPitchDeg)
                );
            }

            private void OrbitWithRightStick(float delta)
            {
                float rotSpeed = Mathf.DegToRad(RightStickRotSpeedDeg);
                _camera.OrbitYawRad += -InputService.RightStick.X * rotSpeed * delta;
                _camera.OrbitPitchRad += -InputService.RightStick.Y * rotSpeed * delta;
                ClampOrbitAngles();
            }

            private void MaintainDistanceAndAutoRotate(float delta)
            {
                var pos = _camera.GlobalPosition;
                var targetPos = _camera.FollowTarget.GlobalPosition;
                var dir = targetPos.DirectionTo(pos);

                _camera.GlobalPosition = targetPos + (dir * FollowDistance);
                _camera.LookAt(targetPos);

                _camera._orbitYawRad = _camera.GlobalRotation.Y;
                _camera._orbitPitchRad = _camera.GlobalRotation.X;
                ClampOrbitAngles();

                _camera.ResetPhysicsInterpolation();
            }

            private void ZoomToFollowDistance(float delta)
            {
                _camera.OrbitDistance = Mathf.MoveToward(
                    _camera.OrbitDistance,
                    FollowDistance,
                    ZoomSpeed * delta
                );
            }
        }

        private partial class SuggestingAngle : OrbitCameraState
        {
            private const float Duration = 0.5f;

            private float _timer;
            private float _initialPitchRad;
            private float _initialYawRad;
            private float _initialDistance;

            public override void OnStateEntered()
            {
                _timer = 0;
                _initialPitchRad = _camera.OrbitPitchRad;
                _initialYawRad = _camera.OrbitYawRad;
                _initialDistance = _camera.OrbitDistance;
            }

            public override void _Process(double deltaD)
            {
                // Move the camera to the suggested angle
                _timer += (float)deltaD;

                float t = _timer / Duration;
                t = Mathf.Min(1, t);

                _camera.OrbitPitchRad = Mathf.LerpAngle(
                    _initialPitchRad,
                    _camera._suggestedPitchRad,
                    t
                );

                _camera.OrbitYawRad = Mathf.LerpAngle(
                    _initialYawRad,
                    _camera._suggestedYawRad,
                    t
                );

                _camera.OrbitDistance = Mathf.Lerp(
                    _initialDistance,
                    _camera._suggestedDistance,
                    t
                );

                _camera.ApplyAnglesAndDistance();

                // ...unless the player has their OWN idea for a camera angle.
                if (InputService.RightStick.Length() > 0.01f)
                    ChangeState<Unlocked>();
            }
        }

        private partial class Recentering : OrbitCameraState
        {
            private const float Duration = 0.1f;

            private float _timer;
            private float _initialPitchRad;
            private float _initialYawRad;

            public override void OnStateEntered()
            {
                _timer = 0;
                _initialPitchRad = _camera.OrbitPitchRad;
                _initialYawRad = _camera.OrbitYawRad;
            }

            public override void _Process(double deltaD)
            {
                _timer += (float)deltaD;

                float t = _timer / Duration;

                _camera.OrbitPitchRad = Mathf.LerpAngle(_initialPitchRad, 0, t);
                _camera.OrbitYawRad = Mathf.LerpAngle(
                    _initialYawRad,
                    _camera.FollowTarget.GlobalRotation.Y,
                    t
                );
                _camera.ApplyAnglesAndDistance();

                if (_timer > Duration)
                {
                    _camera.ForceRecenter();
                    ChangeState<Unlocked>();
                    return;
                }

            }
        }
    }
}

