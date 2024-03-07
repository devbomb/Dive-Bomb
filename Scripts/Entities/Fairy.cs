using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class Fairy : StaticBody3D, IRollable, IKickable
    {
        private readonly StateMachine _stateMachine = new StateMachine(typeof(FairyState));

        private AnimationPlayer Animator => GetNode<AnimationPlayer>("%AnimationPlayer");
        private Node3D Model => GetNode<Node3D>("%Model");
        private Node3D Glass => GetNode<Node3D>("%Glass");
        private GpuParticles3D GlassParticles => GetNode<GpuParticles3D>("%GlassParticles");
        private CollisionShape3D CollisionShape => GetNode<CollisionShape3D>("%CollisionShape");

        private Player Player;

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            AddChild(_stateMachine);
            Reset();
        }

        public void Reset()
        {
            if (SaveFile.Current.CurrentMapProgress.CollectedFairies.Contains(GetSaveKey()))
                _stateMachine.ChangeState<Rescued>();
            else
                _stateMachine.ChangeState<Idle>();
        }

        public string GetSaveKey()
        {
            var builder = new System.Text.StringBuilder();
            Visit(this);
            return builder.ToString();

            void Visit(Node n)
            {
                if (n.GetParent() == GetTree().Root)
                {
                    builder.Append(n.Name);
                    return;
                }

                Visit(n.GetParent());
                builder.Append("/");
                builder.Append(n.GetIndex());
            }
        }

        public void OnRolledInto()
        {
            _stateMachine.ChangeState<Rescuing>();
        }

        public void OnKicked()
        {
            _stateMachine.ChangeState<Rescuing>();
        }

        private partial class Idle : FairyState
        {
            public override void OnStateEntered()
            {
                _fairy.Player = GetTree().FindNode<Player>();
                _fairy.Visible = true;
                _fairy.Animator.Play("PoundingOnGlass");

                _fairy.CollisionShape.Disabled = false;
            }

            public override void OnStateExited()
            {
                _fairy.CollisionShape.Disabled = true;
            }

            public override void _Process(double deltaD)
            {
                _fairy.Model.GlobalRotation = _fairy.GlobalPosition
                    .DirectionTo(_fairy.Player.GlobalPosition)
                    .Flattened()
                    .Normalized()
                    .ForwardToEulerAnglesRad();
            }

            private void OnBodyEntered(Node3D body)
            {
                if (body is Player)
                {
                    ChangeState<Rescuing>();
                }
            }
        }

        private partial class Rescuing : FairyState
        {
            private static float PlayerJumpSpeed => Player.Jump.InitVSpeed / 4;
            private static float PlayerGravity => Player.Jump.ShortHopGravity / 8;

            private bool _wasPlayerOnGround;

            public override void OnStateEntered()
            {
                _wasPlayerOnGround = false;
                SaveFile.Current.CurrentMapProgress.CollectedFairies.Add(_fairy.GetSaveKey());

                // Pause the game (except the player and fairy) during the
                // cutscene to prevent the player from getting hit by enemies.
                // Don't worry, the pause menu won't open if the game is already
                // paused by something else.
                GetTree().Paused = true;
                _fairy.ProcessMode = ProcessModeEnum.Always;
                _fairy.Player.ProcessMode = ProcessModeEnum.Always;

                // Hijack control of the player.  We're going to manipulate them
                // for the cutscene.
                _fairy.Player.ChangeState<PlayerManhandledState>();
                _fairy.Player.Velocity = Vector3.Up * PlayerJumpSpeed;

                // Krrsssh!!!  Shatter the glass!
                _fairy.Glass.Visible = false;
                _fairy.GlassParticles.Emitting = true;

                // And move in for a kiss <3
                _fairy.Animator.Play("Kiss");
            }

            public override void OnStateExited()
            {
                GetTree().Paused = false;
                _fairy.ProcessMode = ProcessModeEnum.Inherit;
                _fairy.Player.ProcessMode = ProcessModeEnum.Inherit;
                _fairy.Player.ChangeState<PlayerWalkState>();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                ApplyGravityToPlayer(delta);
                RotatePlayerTowardFairy(delta);

                if (!_fairy.Animator.IsPlaying())
                    ChangeState<Rescued>();
            }

            private void ApplyGravityToPlayer(float delta)
            {
                _fairy.Player.Velocity += Vector3.Down * PlayerGravity * delta;
                _fairy.Player.MoveAndSlide();

                // HACK: For whatever reason, MoveAndSlide() is causing the
                // player to gain horizontal speed, and I can't figure out why.
                // So, let's just set it to zero.
                _fairy.Player.FSpeed = 0;

                bool onGround = _fairy.Player.IsOnFloor();
                bool wasOnGround = _wasPlayerOnGround;
                _wasPlayerOnGround = onGround;

                if (onGround && !wasOnGround)
                    _fairy.Player.Animator.Play("Idle");
            }

            private void RotatePlayerTowardFairy(float delta)
            {
                if (!_fairy.Player.IsOnFloor())
                    return;

                var targetRotRad = _fairy.Player.GlobalPosition
                    .DirectionTo(_fairy.GlobalPosition)
                    .Flattened()
                    .Normalized()
                    .ForwardToEulerAnglesRad();

                _fairy.Player.GlobalRotation = _fairy.Player.GlobalRotation.DecayTowardsEulerRad(
                    targetRotRad,
                    10,
                    delta
                );
            }
        }

        private partial class Rescued : FairyState
        {
            public override void OnStateEntered()
            {
                _fairy.Visible = false;
            }
        }

        private abstract partial class FairyState : State
        {
            protected Fairy _fairy => _stateMachine.GetParent<Fairy>();
        }
    }
}