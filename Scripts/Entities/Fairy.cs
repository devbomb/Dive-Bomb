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
            _stateMachine.ChangeState<Shattering>();
        }

        public void OnKicked()
        {
            _stateMachine.ChangeState<Shattering>();
        }

        private void SetPausedForCutscene(bool paused)
        {
            GetTree().Paused = paused;

            ProcessMode = paused
                ? ProcessModeEnum.Always
                : ProcessModeEnum.Inherit;

            Player.ProcessMode = ProcessMode;
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
        }

        private partial class Shattering : FairyState
        {
            private static float PlayerJumpSpeed => Player.Jump.InitVSpeed / 4;
            private static float PlayerGravity => Player.Jump.ShortHopGravity / 8;

            private bool _playerLanded;

            public override void OnStateEntered()
            {
                SaveFile.Current.CurrentMapProgress.CollectedFairies.Add(_fairy.GetSaveKey());

                // Pause the game (except the player and fairy) during the
                // cutscene to prevent the player from getting hit by enemies.
                // Don't worry, the pause menu won't open if the game is already
                // paused by something else.
                _fairy.SetPausedForCutscene(true);

                // Hijack control of the player.  We're going to manipulate them
                // for the cutscene.
                _fairy.Player.ChangeState<PlayerManhandledState>();
                _fairy.Player.Velocity = Vector3.Up * PlayerJumpSpeed;
                _playerLanded = false;

                // Krrsssh!!!  Shatter the glass!
                _fairy.Glass.Visible = false;
                _fairy.GlassParticles.Emitting = true;
                _fairy.Animator.Play("Shatter");
            }

            public override void OnStateExited()
            {
                _fairy.SetPausedForCutscene(false);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                ApplyGravityToPlayer(delta);

                if (_playerLanded && !_fairy.Animator.IsPlaying())
                    ChangeState<FlyingToPlayer>();
            }

            private void ApplyGravityToPlayer(float delta)
            {
                _fairy.Player.Velocity += Vector3.Down * PlayerGravity * delta;
                _fairy.Player.MoveAndSlide();

                // HACK: For whatever reason, MoveAndSlide() is causing the
                // player to gain horizontal speed, and I can't figure out why.
                // So, let's just set it to zero.
                _fairy.Player.FSpeed = 0;

                if (_fairy.Player.IsOnFloor() && !_playerLanded)
                {
                    _playerLanded = true;
                    _fairy.Player.Animator.Play("Idle");
                }
            }
        }

        private partial class FlyingToPlayer : FairyState
        {
            private const float Duration = 1;

            private Vector3 _startPos;
            private Vector3 _startRotRad;
            private float _timer;

            public override void OnStateEntered()
            {
                _fairy.SetPausedForCutscene(true);
                _fairy.Animator.Play("Hovering", 0.1f);

                _startPos = _fairy.Model.GlobalPosition;
                _startRotRad = _fairy.Model.GlobalRotation;
                _timer = 0;
            }

            public override void OnStateExited()
            {
                _fairy.SetPausedForCutscene(false);
            }

            public override void _Process(double deltaD)
            {
                _timer += (float)deltaD;

                var targetPos = _fairy.Player.FairyKissPoint.GlobalPosition;
                var targetRotRad = _fairy.Player.FairyKissPoint.GlobalRotation;

                float t = _timer / Duration;
                _fairy.Model.GlobalPosition = _startPos.Lerp(targetPos, t);
                _fairy.Model.GlobalRotation = _startRotRad.LerpEulerRad(targetRotRad, t);

                if (_timer > Duration)
                {
                    _fairy.Model.GlobalPosition = targetPos;
                    _fairy.Model.GlobalRotation = targetRotRad;
                    ChangeState<KissingPlayer>();
                }
            }
        }

        private partial class KissingPlayer : FairyState
        {
            public override void OnStateEntered()
            {
                _fairy.SetPausedForCutscene(true);
                _fairy.Animator.Play("Kiss", 0.3f);
            }

            public override void OnStateExited()
            {
                _fairy.SetPausedForCutscene(false);
                _fairy.Player.ChangeState<PlayerWalkState>();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                if (!_fairy.Animator.IsPlaying())
                    ChangeState<Rescued>();
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