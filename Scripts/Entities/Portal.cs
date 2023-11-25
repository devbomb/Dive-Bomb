using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class Portal : Node3D
    {
        [Export(PropertyHint.File)] public string SkyboxEnvironment;
        [Export(PropertyHint.File)] public string TargetMap;

        [Export] public float ExitAnimationDuration = 1.5f;
        [Export] public float ExitAnimationStartHeight = 2;

        private Node3D _playerSpawn => GetNode<Node3D>("%PlayerSpawnPoint");

        private PortalSurface _surface => GetNode<PortalSurface>("%PortalSurface");

        public override void _Ready()
        {
            _surface.TargetMap = TargetMap;
            _surface.SetSkybox(SkyboxEnvironment);
        }

        public void PlayExitAnimation(double glideAnimationStartTime)
        {
            var player = GetTree().FindNode<Player>();

            player.Animator.Play("Glide", 0);
            player.Animator.Seek(glideAnimationStartTime, true);
            player.AllowInterpolation = false;

            // Warp the player to the start pos of the animation
            player.ChangeState<PlayerManhandledState>();
            player.GlobalRotation = _playerSpawn.GlobalRotation;
            player.GlobalPosition = _playerSpawn.GlobalPosition;
            player.GlobalPosition += Vector3.Up * ExitAnimationStartHeight;
            player.GlobalPosition -= _playerSpawn.GlobalForward() * (player.Camera.OrbitDistance + 2);
            player.ResetPhysicsInterpolation();

            player.Camera.ChangeState<OrbitCameraFreeState>();
            player.Camera.OrbitYawRad = _playerSpawn.GlobalRotation.Y + Mathf.DegToRad(180);
            player.Camera.OrbitPitchRad = 0;

            // Start tweening the player to the spawn point
            var tween = CreateTween();
            tween.TweenProperty(
                player,
                "global_position",
                _playerSpawn.GlobalPosition,
                ExitAnimationDuration
            );

            tween.TweenCallback(Callable.From(FinishExitAnimation));
        }

        private void FinishExitAnimation()
        {
            var player = GetTree().FindNode<Player>();
            player.AllowInterpolation = true;
            player.ResetPhysicsInterpolation();
            player.ForceUpdateTransform();

            player.ChangeState<PlayerWalkState>();
        }
    }
}