using Godot;

namespace FastDragon
{
    // TODO: Don't use inheritance, ya twit!
    public partial class PowerOrb : BreakableStaticBody3D
    {
        [Export] public float SpawnTime = 0.25f;

        public bool IsBroken => _stateMachine.CurrentState is BrokenState;

        private Node3D _model => GetNode<Node3D>("%Model");
        private CollisionShape3D _bodyShape => GetNode<CollisionShape3D>("%BodyShape");
        private Node3D _blobShadow => GetNode<Node3D>("%BlobShadow");
        private GpuParticles3D _explosionParticles => GetNode<GpuParticles3D>("%ExplosionParticles");

        private readonly StateMachine _stateMachine = new StateMachine(typeof(PowerOrbState));

        public override void _Ready()
        {
            AddChild(_stateMachine);

            SignalBus.Instance.LevelReset += Reset;
            Broken += Break;

            Reset();
        }

        private void Reset()
        {
            SetHidden();
        }

        public override void _Process(double deltaD)
        {
            var rot = _model.RotationDegrees;
            rot.Y += 45 * (float)deltaD;
            _model.RotationDegrees = rot;
        }

        public void Reveal()
        {
            _stateMachine.ChangeState<Revealing>();
        }

        public void SetHidden()
        {
            _stateMachine.ChangeState<Hidden>();
        }

        public void Break()
        {
            _stateMachine.ChangeState<BrokenState>();
        }

        private abstract partial class PowerOrbState : State
        {
            protected PowerOrb _self => _stateMachine.GetParent<PowerOrb>();
        }

        private partial class Revealing : PowerOrbState
        {
            private float _animTimer;
            private float _timer;

            public override void OnStateEntered()
            {
                _self._bodyShape.Disabled = true;
                _self._model.Scale = Vector3.Zero;
                _timer = 0;
                _animTimer = 0;

                _self._model.RotationDegrees = new Vector3(0, GD.Randf() * 360, 0);
            }

            public override void OnStateExited()
            {
                _self._bodyShape.Disabled = false;
                _self._model.Scale = Vector3.One;
            }

            public override void _Process(double deltaD)
            {
                _animTimer += (float)deltaD;

                float t = _animTimer / _self.SpawnTime;
                _self._model.Scale = Vector3.Zero.Lerp(Vector3.One, t);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                if (_timer >= _self.SpawnTime)
                    ChangeState<Revealed>();
            }
        }

        private partial class Revealed : PowerOrbState
        {
        }

        private partial class Hidden : PowerOrbState
        {
            public override void OnStateEntered()
            {
                _self._bodyShape.Disabled = true;
                _self._model.Visible = false;
                _self._blobShadow.Visible = false;
            }

            public override void OnStateExited()
            {
                _self._bodyShape.Disabled = false;
                _self._model.Visible = true;
                _self._blobShadow.Visible = true;
            }
        }

        private partial class BrokenState : PowerOrbState
        {
            public override void OnStateEntered()
            {
                _self._explosionParticles.Emitting = true;

                _self._bodyShape.Disabled = true;
                _self._model.Visible = false;
                _self._blobShadow.Visible = false;
            }

            public override void OnStateExited()
            {
                _self._bodyShape.Disabled = false;
                _self._model.Visible = true;
                _self._blobShadow.Visible = true;
            }
        }
    }
}