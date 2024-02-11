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

        private OrbitCameraState _currentState;
        private PhysicsInterpolator3D _interpolator;

        public override void _Ready()
        {
            _interpolator = new PhysicsInterpolator3D();
            AddChild(_interpolator);
            ChangeState<Unlocked>();
        }

        public void ResetPhysicsInterpolation()
        {
            _interpolator.ResetPhysicsInterpolation();
        }

        private void ChangeState<TState>() where TState : OrbitCameraState, new()
        {
            _currentState?.OnStateExited();

            foreach (var state in States())
            {
                state.ProcessMode = ProcessModeEnum.Disabled;
            }

            _currentState = States().FirstOrDefault(s => s is TState);
            if (_currentState == null)
            {
                _currentState = new TState();
                AddChild(_currentState);
            }

            _currentState.ProcessMode = ProcessModeEnum.Inherit;
            _currentState.OnStateEntered();
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

        private IEnumerable<OrbitCameraState> States()
        {
            for (int i = 0; i < GetChildCount(); i++)
            {
                var child = GetChild<Node>(i);

                if (child is OrbitCameraState state)
                    yield return state;
            }
        }

        private partial class OrbitCameraState : Node
        {
            protected OrbitCamera _camera => GetParent<OrbitCamera>();

            public virtual void OnStateEntered() {}
            public virtual void OnStateExited() {}
        }

        private partial class Unlocked : OrbitCameraState
        {
            public float FollowDistance = 6;
            public float ZoomSpeed = 4;

            public float RightStickRotSpeedDeg = 180;

            public float MinOrbitPitchDeg = -89;
            public float MaxOrbitPitchDeg = 0;

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
    }
}

