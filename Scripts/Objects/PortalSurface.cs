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

        private TaskCompletionSource<float> _physicsProcessTcs = new TaskCompletionSource<float>();

        private bool _isControllingPlayer = false;

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
        }

        public override void _Process(double delta)
        {
            _portalCamera.GlobalPosition = _mainCamera.GlobalPosition;
            _portalCamera.GlobalRotation = _mainCamera.GlobalRotation;
            _portalCamera.Environment = Skybox;
        }

        public override void _PhysicsProcess(double delta)
        {
            var tcs = _physicsProcessTcs;
            _physicsProcessTcs = new TaskCompletionSource<float>();
            tcs.SetResult((float)delta);
        }

        private Task<float> NextPhysicsFrame()
        {
            return _physicsProcessTcs.Task;
        }

        private void OnBodyEntered(Node3D body)
        {
            if (body is Player player && !(player.CurrentState is PlayerManhandledState))
            {
                PlayPortalAnimation(player);
            }
        }

        private async void PlayPortalAnimation(Player player)
        {
            player.ChangeState<PlayerManhandledState>();
            player.Animator.Play("Jump");
            player.Velocity = Vector3.Up * Player.Default.JumpVSpeed;
            Vector3 targetRotRad = PlayerTargetRotRad(player);

            while (player.Velocity.Y > 0)
            {
                float delta = await NextPhysicsFrame();
                player.Velocity += Vector3.Down * Player.Default.Gravity * delta;
                player.GlobalPosition += player.Velocity * delta;

                RotatePlayer(delta);
                RecenterCamera(delta);
            }

            player.Animator.Play("Glide");

            while (true)
            {
                float delta = await NextPhysicsFrame();
                player.GlobalPosition += player.GlobalForward() * Player.Glide.Speed * delta;

                RotatePlayer(delta);
                RecenterCamera(delta);
            }

            void RotatePlayer(float delta)
            {
                player.GlobalRotation = player.GlobalRotation.RotateTowardEulerRad(
                    targetRotRad,
                    delta * Mathf.DegToRad(180)
                );
            }

            void RecenterCamera(float delta)
            {
                player.Camera.OrbitYawRad = AngleMath.DecayToward(
                    player.Camera.OrbitYawRad,
                    targetRotRad.Y,
                    5,
                    delta
                );
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
    }
}