using System;
using System.Threading.Tasks;
using Godot;

namespace FastDragon
{
    public partial class PortalSurface : Area3D
    {
        [Export] public Godot.Environment Skybox {get; private set;}

        private Camera3D _portalCamera => GetNode<Camera3D>("%PortalCamera");
        private Camera3D _mainCamera => GetTree().Root.GetCamera3D();

        private StateMachine _stateMachine = new StateMachine(typeof(PortalState));
        private Player _player;
        private Vector3 _playerTargetRotRad;

        public override void _Ready()
        {
            AddChild(_stateMachine);
            _stateMachine.ChangeState<Idle>();

            BodyEntered += OnBodyEntered;
        }

        public override void _Process(double delta)
        {
            _portalCamera.GlobalPosition = _mainCamera.GlobalPosition;
            _portalCamera.GlobalRotation = _mainCamera.GlobalRotation;
            _portalCamera.Environment = Skybox;
        }

        private void OnBodyEntered(Node3D body)
        {
            if (body is Player player && !(player.CurrentState is PlayerManhandledState))
            {
                _player = player;
                _playerTargetRotRad = PlayerTargetRotRad(player);

                if (player.IsOnFloor())
                    _stateMachine.ChangeState<Jumping>();
                else
                    _stateMachine.ChangeState<Flying>();
            }
        }

        private abstract partial class PortalState : State
        {
            protected PortalSurface _portal => _stateMachine.GetParent<PortalSurface>();
            protected Player player => _portal._player;
        }

        private partial class Idle : PortalState {}

        private partial class Jumping : PortalState
        {
            public override void OnStateEntered()
            {
                player.ChangeState<PlayerManhandledState>();
                player.Animator.Play("Jump");
                player.Velocity = Vector3.Up * Player.Default.JumpVSpeed;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                player.Velocity += Vector3.Down * Player.Default.Gravity * delta;
                player.GlobalPosition += player.Velocity * delta;

                _portal.RotatePlayer(delta);
                _portal.RecenterCamera(delta);

                if (player.Velocity.Y <= 0)
                    ChangeState<Flying>();
            }
        }

        private partial class Flying : PortalState
        {
            public override void OnStateEntered()
            {
                player.ChangeState<PlayerManhandledState>();
                player.Animator.Play("Glide");
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                player.GlobalPosition += player.GlobalForward() * Player.Glide.Speed * delta;
                _portal.RotatePlayer(delta);
                _portal.RecenterCamera(delta);
            }
        }

        private Vector3 PlayerTargetRotRad(Player player)
        {
            Vector3 forwardRad = GlobalRotation;
            Vector3 backwardRad = forwardRad + (Vector3.Up * Mathf.DegToRad(180));
            float angleToPlayerRad = (GlobalPosition - player.GlobalPosition)
                .ForwardToEulerAnglesRad()
                .Y;

            float diffToForwardRad = AngleMath.Difference(angleToPlayerRad, forwardRad.Y);
            float diffToBackwardRad = AngleMath.Difference(angleToPlayerRad, backwardRad.Y);

            return (Mathf.Abs(diffToForwardRad) < Mathf.Abs(diffToBackwardRad))
                ? forwardRad
                : backwardRad;
        }

        private void RotatePlayer(float delta)
        {
            _player.GlobalRotation = _player.GlobalRotation.RotateTowardEulerRad(
                _playerTargetRotRad,
                delta * Mathf.DegToRad(180)
            );
        }

        private void RecenterCamera(float delta)
        {
            _player.Camera.OrbitYawRad = AngleMath.DecayToward(
                _player.Camera.OrbitYawRad,
                _playerTargetRotRad.Y,
                5,
                delta
            );
        }
    }
}