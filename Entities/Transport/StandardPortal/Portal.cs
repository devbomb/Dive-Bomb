using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class Portal : Node3D
    {
        [Export(PropertyHint.File)] public string SkyboxEnvironment;
        [Export(PropertyHint.File)] public string TargetLevel;
        [Export] public string Text;

        [Export] public float ExitAnimationDuration = 2.5f;
        [Export] public float ExitAnimationStartHeight = 0;
        [Export] public float ExitAnimationParabolaHeight = 2;

        public Node3D PlayerSpawn => GetNode<Node3D>("%PlayerSpawnPoint");

        private PortalSurface _surface => GetNode<PortalSurface>("%PortalSurface");

        private MeshLabel3D _frontLabel => GetNode<MeshLabel3D>("%FrontLabel");
        private MeshLabel3D _backLabel => GetNode<MeshLabel3D>("%BackLabel");

        private Vector3 _exitAnimationStartPos;
        private float _exitAnimationTimer;
        private bool _playingExitAnimation = false;

        public override void _Ready()
        {
            _surface.TargetLevel = TargetLevel;
            _surface.SetSkybox(SkyboxEnvironment);

            _frontLabel.Text = Text;
            _backLabel.Text = Text;
        }

        public void PlayExitAnimation()
        {
            var player = GetTree().FindNode<Player>();

            // Warp the player to the start pos of the animation
            _exitAnimationStartPos = PlayerSpawn.GlobalPosition;
            _exitAnimationStartPos += Vector3.Up * ExitAnimationStartHeight;
            _exitAnimationStartPos -= PlayerSpawn.GlobalForward() * (player.Camera.OrbitDistance + 2);

            player.SetVisibleInPortals(true);
            player.ChangeState<PlayerManhandledState>();
            player.GlobalRotation = PlayerSpawn.GlobalRotation;
            player.GlobalPosition = _exitAnimationStartPos;
            player.ResetPhysicsInterpolation3D();

            player.CameraFocus.Reset();

            player.Camera.OrbitYawRad = PlayerSpawn.GlobalRotation.Y + Mathf.DegToRad(180);
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
                PlayerSpawn.GlobalPosition,
                ExitAnimationParabolaHeight,
                t
            );

            if (_exitAnimationTimer > ExitAnimationDuration)
            {
                _playingExitAnimation = false;
                player.GlobalPosition = PlayerSpawn.GlobalPosition;
                player.ResetPhysicsInterpolation3D();
                player.ChangeState<PlayerWalkState>();
                player.SetVisibleInPortals(false);
            }
        }
    }
}