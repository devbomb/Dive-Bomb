using Godot;

namespace FastDragon
{
    [Tool]
    public partial class FlameTendril : Node3D
    {
        [Export] public float MaxLength
        {
            get => _maxLength;
            set => SetProperty(ref _maxLength, value);
        }
        private float _maxLength = 1.5f;

        [Export] public float Radius
        {
            get => _radius;
            set => SetProperty(ref _radius, value);
        }
        private float _radius = 0.25f;

        public Node3D BodyToIgnore;
        public float ActiveDuration;

        private Node3D _sphere => GetNode<Node3D>("%Sphere");
        private Node3D _tendrilScaler => GetNode<Node3D>("%TendrilParticlesScaler");
        private GpuParticles3D _particles => GetNode<GpuParticles3D>("%FlameParticles");
        private GpuParticles3D _sphereParticles => GetNode<GpuParticles3D>("%SphereParticles");
        private ParticleProcessMaterial _sphereParticlesProc => (ParticleProcessMaterial)_sphereParticles.ProcessMaterial;

        private PhysicsBody3D _body => GetNode<PhysicsBody3D>("%Body");
        private CollisionShape3D _bodyShape => GetNode<CollisionShape3D>("%BodyShape");

        private bool _initialized = false;

        private bool _active = false;
        private float _timer = 0;
        private float _length;


        public override void _Ready()
        {
            if (Engine.IsEditorHint())
            {
                _initialized = true;
                UpdateSize(_maxLength);
            }
        }

        public override void _Process(double deltaD)
        {
            if (Engine.IsEditorHint())
                return;

            _sphereParticles.Emitting = _active;
            _particles.Emitting = _active;

            // Do a sphere-cast up to the intended lenght, and then adjust
            // the visuals according to how far it went.
            var collision = CastToLength();

            float effectiveLength = collision != null
                ? collision.GetTravel().Length()
                : _length;

            UpdateSize(effectiveLength);
        }

        public override void _PhysicsProcess(double deltaD)
        {
            if (Engine.IsEditorHint())
                return;

            float delta = (float)deltaD;

            if (!_active)
                return;

            // Move the tendril forward
            _timer += delta;
            _length = Mathf.Lerp(0, MaxLength, _timer / ActiveDuration);
            _length = Mathf.Min(_length, MaxLength);

            // Flame things that we collide with
            var collision = CastToLength();

            if (collision == null)
                return;

            if (collision.GetCollider() is IFlamable flamable)
            {
                flamable.OnFlamed();
            }

            // Stop when the timer is up
            if (_timer >= ActiveDuration)
                Stop();
        }

        public void Start()
        {
            _particles.Restart();
            _sphereParticles.Restart();
            _length = 0;
            _timer = 0;
            _active = true;
        }

        public void Stop()
        {
            _active = false;
        }

        private void UpdateSize(float length)
        {
            _sphereParticlesProc.ScaleMin = _radius;
            _sphereParticlesProc.ScaleMax = _radius;
            _sphere.Position = Vector3.Forward * length;
            _tendrilScaler.Scale = new Vector3(1, 1, length);

            var procMat = (ParticleProcessMaterial)_particles.ProcessMaterial;
            procMat.ScaleMin = _radius;
            procMat.ScaleMax = _radius;

            var shape = (SphereShape3D)_bodyShape.Shape;
            shape.Radius = _radius;
        }

        private void SetProperty(ref float storage, float value)
        {
            storage = value;

            if (Engine.IsEditorHint() && _initialized && Owner != this)
                UpdateSize(_maxLength);
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