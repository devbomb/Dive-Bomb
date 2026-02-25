using System.Linq;
using Godot;

namespace FastDragon
{
    // TODO: Refactor DivableWall to use this, instead of duplicating it.
    [GlobalClass]
    public partial class ShatterableWall : BreakableStaticBody3D
    {
        [Export] public GpuParticles3D ShatterPartciles;
        [Export] public float ShatterDuration = 4f / 60;
        [Export] public double HitStopDuration = 0.2;

        private readonly MeshExploder _meshExploder = new();
        private readonly StateMachine _stateMachine = new();

        private MeshInstance3D _meshInstance;

        public override void _Ready()
        {
            _meshInstance = this.FindNode<MeshInstance3D>();
            AddChild(_stateMachine);
            AddChild(_meshExploder);

            Broken += StartShattering;

            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        public void Reset()
        {
            _stateMachine.ChangeState<Solid>();
        }

        private void StartShattering()
        {
            _stateMachine.ChangeState<Shattering>();
        }

        private void SetCollisionEnabled(bool enabled)
        {
            var shapes = this.EnumerateDescendantsOfType<CollisionShape3D>();
            foreach (var shape in shapes)
                shape.Disabled = !enabled;
        }

        private class Solid : State<ShatterableWall>
        {
            public override void OnStateEntered()
            {
                Self.SetCollisionEnabled(true);
                Self._meshInstance.Visible = true;
            }
        }

        private class Shattering : State<ShatterableWall>
        {
            private const float EndScale = 4f;

            private float _timer;

            public override void OnStateEntered()
            {
                HitStopManager.Instance.StopFor(Self.HitStopDuration);

                Self.SetCollisionEnabled(false);
                Self._meshInstance.Visible = false;
                _timer = 0;

                Self._meshExploder.Explode(
                    Self._meshInstance,
                    GetTree().FindNode<Player>().GlobalPosition,
                    EndScale,
                    Self.ShatterDuration
                );

                if (Self.ShatterPartciles != null)
                    SpawnParticles();
            }

            public override void OnStateExited()
            {
                if (Self.ShatterPartciles != null)
                    Self.ShatterPartciles.Emitting = false;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                if (_timer >= Self.ShatterDuration)
                {
                    ChangeState<BrokenState>();
                }
            }

            private void SpawnParticles()
            {
                Self.ShatterPartciles.Restart();
                Self.ShatterPartciles.Emitting = true;

                // Walls can be large.  If we always spawn the particles at the
                // origin point, it could be far away from where the player actually
                // hit, which would look weird.  Therefore, let's spawn the
                // particles at the player's position.
                var player = GetTree().FindNode<Player>();
                Self.ShatterPartciles.GlobalPosition = player.GlobalPosition;
                Self.ShatterPartciles.GlobalPosition += Vector3.Up;
            }
        }

        private class BrokenState : State<ShatterableWall>
        {
            public override void OnStateEntered()
            {
                Self.SetCollisionEnabled(false);
                Self._meshInstance.Visible = false;
            }
        }
    }
}