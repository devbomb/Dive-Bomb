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
        private float _timeToTop;
        private float _timer;
        private Vector3 _playerStartPos;
        private Vector3 _playerStartRotRad;

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            // This logic can't be put in _Ready() because the corresponding
            // WhirlwindTop node might not be instantiated until _after_ this
            // node.
            if (_top == null)
                FindTop();

            if (_ensaredPlayer == null)
                return;

            _timer += delta;

            if (_timer > _timeToTop)
            {
                _ensaredPlayer.GlobalPosition = _top.GlobalPosition;
                _ensaredPlayer.GlobalRotation = _top.GlobalRotation;
                _ensaredPlayer.ChangeState<PlayerGlideState>();
                _ensaredPlayer = null;
                return;
            }

            float t = _timer / _timeToTop;
            _ensaredPlayer.GlobalPosition = _playerStartPos.Lerp(_top.GlobalPosition, t);
            _ensaredPlayer.GlobalRotation = _playerStartRotRad.LerpEulerRad(_top.GlobalRotation, t);
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

            _playerStartPos = player.GlobalPosition;
            _playerStartRotRad = player.GlobalRotation;

            float initialHeight = _top.GlobalPosition.Y - player.GlobalPosition.Y;
            _timeToTop = initialHeight / WhirlwindSpeed;
            _timer = 0;
        }
    }
}