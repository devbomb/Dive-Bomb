using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class OrbitCamera : Camera3D
    {
        [Export] public Node3D FollowTarget;

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

        /// <summary>
        /// This height gets added to the camera's final position, AFTER the
        /// orbit angles are applied and AFTER the camera has been aimed at the
        /// target.
        /// </summary>
        public float CameraHeightOffset = 2;

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

        private void ApplyAnglesAndDistance()
        {
            Vector3 dir = Vector3.Back
                .Rotated(Vector3.Right, OrbitPitchRad)
                .Rotated(Vector3.Up, OrbitYawRad);

            Vector3 offset = dir * OrbitDistance;
            GlobalPosition = FollowTarget.GlobalPosition + offset;
            LookAt(FollowTarget.GlobalPosition);

            GlobalPosition += Vector3.Up * CameraHeightOffset;

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
                    return;

                float rotSpeed = Mathf.DegToRad(RightStickRotSpeedDeg);
                _camera.OrbitYawRad += -InputService.RightStick.X * rotSpeed * delta;
                _camera.OrbitPitchRad += -InputService.RightStick.Y * rotSpeed * delta;
                ClampOrbitAngles();

                _camera.OrbitDistance = Mathf.MoveToward(
                    _camera.OrbitDistance,
                    FollowDistance,
                    ZoomSpeed * delta
                );
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

