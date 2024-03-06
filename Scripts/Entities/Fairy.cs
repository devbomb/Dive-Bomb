using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class Fairy : Node3D
    {
        private readonly StateMachine _stateMachine = new StateMachine(typeof(FairyState));

        private AnimationPlayer Animator => GetNode<AnimationPlayer>("%AnimationPlayer");
        private Node3D Model => GetNode<Node3D>("%Model");
        private Area3D PlayerDetector => GetNode<Area3D>("%PlayerDetector");

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

        private partial class Idle : FairyState
        {
            public override void OnStateEntered()
            {
                _fairy.Player = GetTree().FindNode<Player>();
                _fairy.Visible = true;
                _fairy.PlayerDetector.BodyEntered += OnBodyEntered;
                _fairy.Animator.Play("PoundingOnGlass");

            }

            public override void OnStateExited()
            {
                _fairy.PlayerDetector.BodyEntered -= OnBodyEntered;
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
            private float _timer;

            public override void OnStateEntered()
            {
                SaveFile.Current.CurrentMapProgress.CollectedFairies.Add(_fairy.GetSaveKey());

                _fairy.Player.ChangeState<PlayerManhandledState>();
                _fairy.Player.GlobalRotation = _fairy.Player
                    .GlobalPosition
                    .DirectionTo(_fairy.GlobalPosition)
                    .Flattened()
                    .Normalized()
                    .ForwardToEulerAnglesRad();
                _fairy.Player.ResetPhysicsInterpolation();

                _timer = 3;
            }

            public override void OnStateExited()
            {
                _fairy.Player?.ChangeState<PlayerWalkState>();
                _fairy.Player = null;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer -= (float)deltaD;

                if (_timer <= 0)
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