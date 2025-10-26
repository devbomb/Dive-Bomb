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

        private readonly StateMachine _stateMachine = new();

        public Portal()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            _surface.TargetLevel = TargetLevel;
            _surface.SetSkybox(SkyboxEnvironment);

            _frontLabel.Text = Text;
            _backLabel.Text = Text;

            _stateMachine.ChangeState<Idle>();
        }

        public void PlayExitAnimation()
        {
            _stateMachine.ChangeState<Exiting>();
        }

        private class Idle : State<Portal>
        {

        }

        private class Exiting : State<Portal>
        {
            private Vector3 _exitAnimationStartPos;
            private double _timer;

            public override void OnStateEntered()
            {
                var player = GetTree().FindNode<Player>();
                _timer = 0;

                // Warp the player to the start pos of the animation
                _exitAnimationStartPos = Self.PlayerSpawn.GlobalPosition;
                _exitAnimationStartPos += Vector3.Up * Self.ExitAnimationStartHeight;
                _exitAnimationStartPos -= Self.PlayerSpawn.GlobalForward() * (player.Camera.OrbitDistance + 2);

                player.SetVisibleInPortals(true);
                player.ChangeState<PlayerManhandledState>();
                player.GlobalRotation = Self.PlayerSpawn.GlobalRotation;
                player.GlobalPosition = _exitAnimationStartPos;
                player.ResetPhysicsInterpolation3D();

                player.CameraFocus.Reset();

                player.Camera.OrbitYawRad = Self.PlayerSpawn.GlobalRotation.Y + Mathf.DegToRad(180);
                player.Camera.OrbitPitchRad = 0;
            }

            public override void OnStateExited()
            {
                var player = GetTree().FindNode<Player>();
                player.SetVisibleInPortals(false);
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;

                float t = (float)_timer / Self.ExitAnimationDuration;

                var player = GetTree().FindNode<Player>();
                player.GlobalPosition = _exitAnimationStartPos.LerpParabola(
                    Self.PlayerSpawn.GlobalPosition,
                    Self.ExitAnimationParabolaHeight,
                    t
                );

                if (_timer > Self.ExitAnimationDuration)
                {
                    player.GlobalPosition = Self.PlayerSpawn.GlobalPosition;
                    player.ResetPhysicsInterpolation3D();
                    player.ChangeState<PlayerWalkState>();

                    ChangeState<Idle>();
                }
            }
        }
    }
}