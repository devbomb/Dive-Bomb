using System;
using Godot;

namespace FastDragon
{
    public partial class InterpolatedCharacterBody3D : CharacterBody3D
    {
        public bool AllowInterpolation
        {
            get => _interpolator.AllowInterpolation;
            set => _interpolator.AllowInterpolation = value;
        }

        private PhysicsInterpolator3D _interpolator;

        public override void _Ready()
        {
            _interpolator = new PhysicsInterpolator3D();
            AddChild(_interpolator);
        }

        public void ResetPhysicsInterpolation()
        {
            _interpolator.ResetPhysicsInterpolation();
        }
    }
}