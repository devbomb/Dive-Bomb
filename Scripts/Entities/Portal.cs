using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class Portal : Node3D
    {
        [Export(PropertyHint.File)] public string SkyboxEnvironment;
        [Export(PropertyHint.File)] public string TargetMap;

        [Export] public float ExitAnimationDuration = 1.5f;
        [Export] public float ExitAnimationStartHeight = 0;
        [Export] public float ExitAnimationParabolaHeight = 2;

        private Node3D _playerSpawn => GetNode<Node3D>("%PlayerSpawnPoint");
        private PortalSurface _surface => GetNode<PortalSurface>("%PortalSurface");

        private Vector3 _exitAnimationStartPos;
        private float _exitAnimationTimer;
        private bool _playingExitAnimation = false;

        public override void _Ready()
        {
            _surface.TargetMap = TargetMap;
            _surface.SetSkybox(SkyboxEnvironment);
        }

        public void PlayExitAnimation()
        {
            var player = GetTree().FindNode<Player>();
            player.Animator.Play(
                "Roll",
                customBlend: 0.5f,
                customSpeed: 2
            );

            // Warp the player to the start pos of the animation
            _exitAnimationStartPos = _playerSpawn.GlobalPosition;
            _exitAnimationStartPos += Vector3.Up * ExitAnimationStartHeight;
            _exitAnimationStartPos -= _playerSpawn.GlobalForward() * (player.Camera.OrbitDistance + 2);

            player.ChangeState<PlayerManhandledState>();
            player.GlobalRotation = _playerSpawn.GlobalRotation;
            player.GlobalPosition = _exitAnimationStartPos;
            player.ResetPhysicsInterpolation();

            player.Camera.OrbitYawRad = _playerSpawn.GlobalRotation.Y + Mathf.DegToRad(180);
            player.Camera.OrbitPitchRad = 0;

            // Start tweening the player to the spawn point
            _exitAnimationTimer = 0;
            _playingExitAnimation = true;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            if (!_playingExitAnimation)
                return;

            _exitAnimationTimer += delta;
            float t = _exitAnimationTimer / ExitAnimationDuration;

            var player = GetTree().FindNode<Player>();
            player.GlobalPosition = _exitAnimationStartPos.LerpParabola(
                _playerSpawn.GlobalPosition,
                ExitAnimationParabolaHeight,
                t
            );

            if (_exitAnimationTimer > ExitAnimationDuration)
            {
                _playingExitAnimation = false;
                player.GlobalPosition = _playerSpawn.GlobalPosition;
                player.ResetPhysicsInterpolation();
                player.ChangeState<PlayerWalkState>();
            }
        }
    }
}