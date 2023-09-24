using Godot;

namespace FastDragon
{
    [Tool]
    public partial class FlameTendril : Node3D
    {
        [Export] public float Length
        {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        [Export] public float Radius
        {
            get => _radius;
            set => SetProperty(ref _radius, value);
        }

        [Export] public float ParticleSpeed
        {
            get => _particleSpeed;
            set => SetProperty(ref _particleSpeed, value);
        }

        public Node3D BodyToIgnore;

        private float _length = 1.5f;
        private float _radius = 0.25f;
        private float _particleSpeed = 1;

        private Node3D _sphere => GetNode<Node3D>("%Sphere");
        private GpuParticles3D _particles => GetNode<GpuParticles3D>("%FlameParticles");

        private PhysicsBody3D _body => GetNode<PhysicsBody3D>("%Body");
        private CollisionShape3D _bodyShape => GetNode<CollisionShape3D>("%BodyShape");

        public override void _Process(double deltaD)
        {
            if (Engine.IsEditorHint())
                return;

            // Do a sphere-cast up to the intended lenght, and then adjust
            // the visuals according to how far it went.
            var collision = CastToLength();

            float effectiveLength = collision != null
                ? collision.GetTravel().Length()
                : _length;

            UpdateSize(effectiveLength);
        }

        public void Start()
        {
            _particles.Restart();
            _particles.Emitting = true;
            _sphere.Visible = true;
        }

        public void Stop()
        {
            _particles.Emitting = false;
            _sphere.Visible = false;
        }

        private void UpdateSize(float length)
        {
            _sphere.Scale = Vector3.One * _radius;
            _sphere.Position = Vector3.Forward * length;
            _particles.Lifetime = length / _particleSpeed;

            var procMat = (ParticleProcessMaterial)_particles.ProcessMaterial;
            procMat.InitialVelocityMin = _particleSpeed;
            procMat.InitialVelocityMax = _particleSpeed;

            var shape = (SphereShape3D)_bodyShape.Shape;
            shape.Radius = _radius;
        }

        private void SetProperty(ref float storage, float value)
        {
            storage = value;

            if (Engine.IsEditorHint())
                UpdateSize(_length);
        }

        private KinematicCollision3D CastToLength()
        {
            _body.AddCollisionExceptionWith(BodyToIgnore);

            var result = _body.MoveAndCollide(
                this.GlobalForward() * _length,
                testOnly: true,
                recoveryAsCollision: true
            );

            _body.RemoveCollisionExceptionWith(BodyToIgnore);

            return result;
        }
    }
}