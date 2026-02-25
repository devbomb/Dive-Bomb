using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class DivableWall : StaticBody3D, IBreakable
    {
        private static readonly PackedScene FxScene =
            ResourceLoader.Load<PackedScene>("res://Entities/Wall/DivableWall/DivableWallFX.tscn");

        public bool VulnerableToKick => false;
        public float CameraShakeMagnitude => 0.5f;

        private readonly DivableWallFX _fx = FxScene.Instantiate<DivableWallFX>();
        private readonly MeshExploder _meshExploder = new();
        private readonly StateMachine _stateMachine = new();

        private MeshInstance3D _meshInstance;

        public DivableWall()
        {
            AddChild(_stateMachine);
            AddChild(_meshExploder);
            AddChild(_fx);
        }

        public override void _Ready()
        {
            _meshInstance = this.EnumerateChildren()
                .OfType<MeshInstance3D>()
                .First();

            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        public void Reset()
        {
            _stateMachine.ChangeState<Solid>();
        }

        public void OnBroken()
        {
            _stateMachine.ChangeState<Shattering>();
        }

        private void SetCollisionEnabled(bool enabled)
        {
            var shapes = this.EnumerateDescendantsOfType<CollisionShape3D>();
            foreach (var shape in shapes)
                shape.Disabled = !enabled;
        }

        private class Solid : State<DivableWall>
        {
            public override void OnStateEntered()
            {
                Self.SetCollisionEnabled(true);
                Self._meshInstance.Visible = true;
            }
        }

        private class Shattering : State<DivableWall>
        {
            private const float Duration = 4f / 60;
            private const float EndScale = 4f;
            private const float HitStopDuration = 0.2f;

            private float _timer;

            public override void OnStateEntered()
            {
                HitStopManager.Instance.StopFor(HitStopDuration);

                Self.SetCollisionEnabled(false);
                Self._meshInstance.Visible = false;
                _timer = 0;

                Self._meshExploder.Explode(
                    Self._meshInstance,
                    GetTree().FindNode<Player>().GlobalPosition,
                    EndScale,
                    Duration
                );

                Self._fx.Play();
            }

            public override void OnStateExited()
            {
                Self._fx.Stop();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                if (_timer >= Duration)
                {
                    ChangeState<Broken>();
                }
            }
        }

        private class Broken : State<DivableWall>
        {
            public override void OnStateEntered()
            {
                Self.SetCollisionEnabled(false);
                Self._meshInstance.Visible = false;
            }
        }
    }
}