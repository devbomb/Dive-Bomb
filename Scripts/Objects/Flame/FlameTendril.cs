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

        private Node3D _cap => GetNode<Node3D>("%Cap");
        private Node3D _stalk => GetNode<Node3D>("%Stalk");
        private GpuParticles3D _capParticles => GetNode<GpuParticles3D>("%CapParticles");
        private GpuParticles3D _stalkParticles => GetNode<GpuParticles3D>("%StalkParticles");
        private GpuParticles3D _hitSmokePartciles => GetNode<GpuParticles3D>("%HitSmokeParticles");
        private ParticleProcessMaterial _capParticlesProc => (ParticleProcessMaterial)_capParticles.ProcessMaterial;

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

            _capParticles.Emitting = _active;
            _stalkParticles.Emitting = _active;

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

            if (collision?.GetCollider() is IFlamable flamable)
            {
                flamable.OnFlamed();
            }

            // Stop early if we hit something
            if (collision != null)
            {
                Stop();
                _hitSmokePartciles.GlobalPosition = GlobalPosition + (this.GlobalForward() * _length);
                _hitSmokePartciles.Restart();
                return;
            }

            // Stop when the timer is up, or when we hit something
            if (_timer >= ActiveDuration)
                Stop();
        }

        public void Start()
        {
            _stalkParticles.Restart();
            _capParticles.Restart();
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
            _cap.Position = Vector3.Forward * length;

            // HACK: There appears to be some kind of Godot bug where, if you
            // set a GPUParticles3D's scale to (1, 1, 0), it will occasionally
            // (but not always) spew out a bunch of "det == 0" errors.
            //
            // To avoid this, just make it invisible instead of changing the
            // scale, if the scale is going to be 0.
            if (length > 0)
                _stalk.Scale = new Vector3(1, 1, length);
            _stalk.Visible = length > 0;

            _capParticlesProc.ScaleMin = _radius;
            _capParticlesProc.ScaleMax = _radius;

            var procMat = (ParticleProcessMaterial)_stalkParticles.ProcessMaterial;
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