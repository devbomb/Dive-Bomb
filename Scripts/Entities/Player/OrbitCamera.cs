using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class OrbitCamera : Camera3D
    {
        [Export] public Node3D FollowTarget;

        public float OrbitDistance = 6;
        public float OrbitYawRad;
        public float OrbitPitchRad;

        private OrbitCameraState _currentState;

        public override void _Ready()
        {
            ChangeState<OrbitCameraFreeState>();
        }

        public void ChangeState<TState>() where TState : OrbitCameraState
        {
            _currentState?.OnStateExited();

            foreach (var state in States())
            {
                state.ProcessMode = ProcessModeEnum.Disabled;
            }

            _currentState = States().First(s => s is TState);
            _currentState.ProcessMode = ProcessModeEnum.Inherit;
            _currentState.OnStateEntered();
        }

        public override void _Process(double deltaD)
        {
            ApplyAnglesAndDistance();
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
    }
}

