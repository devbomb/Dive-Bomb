using System;
using Godot;

namespace FastDragon
{
    public partial class WhirlwindBottom : Area3D
    {
        private const float WhirlwindSpeed = 4;

        private RayCast3D _heightRay => GetNode<RayCast3D>("%HeightRay");
        private GpuParticles3D _sparkles => GetNode<GpuParticles3D>("%Sparkles");
        private GpuParticles3D _lines => GetNode<GpuParticles3D>("%Lines");
        private CollisionShape3D _shape => GetNode<CollisionShape3D>("%Shape");


        private WhirlwindTop _top = null;
        private Player _ensaredPlayer = null;

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
        }

        public override void _PhysicsProcess(double delta)
        {
            // This logic can't be put in _Ready() because the corresponding
            // WhirlwindTop node might not be instantiated until _after_ this
            // node.
            if (_top == null)
                FindTop();

            if (_ensaredPlayer == null)
                return;

            _ensaredPlayer.MoveAndSlide();

            if (_ensaredPlayer.GlobalPosition.Y >= _top.GlobalPosition.Y)
            {
                _ensaredPlayer.ChangeState<PlayerGlideState>();
                _ensaredPlayer = null;
            }
        }

        private void FindTop()
        {
            _heightRay.ForceRaycastUpdate();

            if (!(_heightRay.GetCollider() is WhirlwindTop top))
                throw new Exception("No top detected for this whirlwind");

            _top = top;

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

        private void OnBodyEntered(Node body)
        {
            if (!(body is Player player))
                return;

            if (player.CurrentState is PlayerManhandledState)
                return;

            _ensaredPlayer = player;
            player.ChangeState<PlayerManhandledState>();
            player.Camera.ChangeState<OrbitCameraFreeState>();
            player.Velocity = Vector3.Up * WhirlwindSpeed;

            // TODO: smoothly move the player to the center, instead of
            // teleporting them
            var pos = player.GlobalPosition;
            pos.X = GlobalPosition.X;
            pos.Z = GlobalPosition.Z;
            player.GlobalPosition = pos;

            // TODO: Smoothly rotate the player throughout the sequence, instead
            // of right now.
            player.GlobalRotation = GlobalRotation;

            player.ResetPhysicsInterpolation();
        }
    }
}