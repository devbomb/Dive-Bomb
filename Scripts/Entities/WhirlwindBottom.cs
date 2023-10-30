using System;
using Godot;

namespace FastDragon
{
    public partial class WhirlwindBottom : Area3D
    {
        private RayCast3D _heightRay => GetNode<RayCast3D>("%HeightRay");
        private GpuParticles3D _sparkles => GetNode<GpuParticles3D>("%Sparkles");
        private GpuParticles3D _lines => GetNode<GpuParticles3D>("%Lines");
        private CollisionShape3D _shape => GetNode<CollisionShape3D>("%Shape");

        private bool _initialized = false;

        public override void _PhysicsProcess(double delta)
        {
            // This logic can't be put in _Ready() because the corresponding
            // WhirlwindTop node might not be instantiated until _after_ this
            // node.
            if (!_initialized)
                Initialize();
        }

        private void Initialize()
        {
            _initialized = true;

            _heightRay.ForceRaycastUpdate();

            if (!(_heightRay.GetCollider() is WhirlwindTop top))
                throw new Exception("No top detected for this whirlwind");

            GlobalRotation = top.GlobalRotation;

            float height = top.GlobalPosition.Y - GlobalPosition.Y;
            SetHeight(_sparkles, height);
            SetHeight(_lines, height);

            var cylinder = (CylinderShape3D)_shape.Shape;
            cylinder.Height = height;
            _shape.Position = Vector3.Up * height / 2;
        }

        private void SetHeight(GpuParticles3D particles, float height)
        {
            var processMat = (ParticleProcessMaterial)particles.ProcessMaterial;
            float speed = processMat.InitialVelocityMax;
            particles.Lifetime = height / speed;
        }
    }
}