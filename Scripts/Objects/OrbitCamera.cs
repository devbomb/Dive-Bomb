using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class OrbitCamera : Camera3D
    {
        [Export] public Node3D FollowTarget;

        public float FollowDistance = 6;
        public float ZoomSpeed = 4;

        public float RightStickRotSpeedDeg = 180;

        public float MinOrbitPitchDeg = -89;
        public float MaxOrbitPitchDeg = 0;


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

        private void ApplyAnglesAndDistance()
        {
            Vector3 dir = Vector3.Back
                .Rotated(Vector3.Right, OrbitPitchRad)
                .Rotated(Vector3.Up, OrbitYawRad);

            Vector3 offset = dir * OrbitDistance;
            GlobalPosition = FollowTarget.GlobalPosition + offset;
            LookAt(FollowTarget.GlobalPosition);
        }

        private void ClampOrbitAngles()
        {
            OrbitYawRad = Mathf.PosMod(OrbitYawRad, Mathf.DegToRad(360));
            OrbitPitchRad = Mathf.Clamp(
                OrbitPitchRad,
                Mathf.DegToRad(MinOrbitPitchDeg),
                Mathf.DegToRad(MaxOrbitPitchDeg)
            );
        }
    }
}

