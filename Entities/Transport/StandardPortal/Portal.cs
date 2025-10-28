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

        [Export] public Node3D PlayerSpawn;

        [ExportGroup("Internal")]
        [Export] public PortalSurface PortalSurface;

        [ExportSubgroup("Front")]
        [Export] public MeshLabel3D FrontLabel;
        [Export] public Node3D PlayerEnterFrontPoint;
        [Export] public PathFollow3D CameraEnterFrontPath;

        [ExportSubgroup("Back")]
        [Export] public MeshLabel3D BackLabel;
        [Export] public Node3D PlayerEnterBackPoint;
        [Export] public PathFollow3D CameraEnterBackPath;

        private readonly StateMachine _stateMachine = new();

        private Environment _skyboxEnvironment;
        private Vector3 _playerTargetRotRad;

        public Portal()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            _skyboxEnvironment = ResourceLoader.Load<Environment>(SkyboxEnvironment);
            PortalSurface.SetSkybox(_skyboxEnvironment);

            FrontLabel.Text = Text;
            BackLabel.Text = Text;

            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            GetTree().FindNode<Player>().SetVisibleInPortals(false);
            _stateMachine.ChangeState<Idle>();
        }

        public void OnBodyEntered(Node3D body)
        {
            if (body is Player player && !(player.CurrentState is PlayerManhandledState))
            {
                PlayEnterAnimation(player);
            }
        }

        public void PlayEnterAnimation(Player player)
        {
            _stateMachine.ChangeState<Entering>();
        }

        public void PlayExitAnimation()
        {
            _stateMachine.ChangeState<Exiting>();
        }

        private class Idle : State<Portal>
        {
        }

        private class Entering : State<Portal>
        {
            private const double Duration = 1;

            private Player _player;
            private Transform3D _playerStartPos;
            private Transform3D _playerTrajectoryPos;
            private Transform3D _playerTargetPos;

            private PathFollow3D _cameraPath;

            private double _timer;

            public override void OnStateEntered()
            {
                _player = GetTree().FindNode<Player>();
                _player.ChangeState<PlayerManhandledState>();
                _player.SetVisibleInPortals(true);
                _playerStartPos = _player.GlobalTransform;
                _playerTrajectoryPos = _player.GlobalTransform;

                // Force the player into a diving-like motion if they weren't
                // already diving to begin with
                if (_player.Animator.CurrentAnimation != "Dive")
                {
                    _player.Animator.Play("Dive");
                    _player.VSpeed = Player.Dive.InitialVSpeed;
                    _player.FSpeed = Player.Dive.FSpeed;
                }

                // Disable the player's collision so they don't die when this
                // animation inevitably takes them out of bounds
                _player.BodyCollisionShape.Disabled = true;

                _player.Camera.StartManhandling(_player.Camera.GlobalTransform, (float)Duration);

                if (IsEnteringFromFront(_player.Velocity))
                {
                    _cameraPath = Self.CameraEnterFrontPath;
                    _playerTargetPos = Self.PlayerEnterFrontPoint.GlobalTransform;
                }
                else
                {
                    _cameraPath = Self.CameraEnterBackPath;
                    _playerTargetPos = Self.PlayerEnterBackPoint.GlobalTransform;
                }

                _cameraPath.ProgressRatio = 0;

                _timer = 0;
            }

            public override void OnStateExited()
            {
                Self.FrontLabel.Scale = Vector3.One;
                Self.BackLabel.Scale = Vector3.One;

                _player.SetVisibleInPortals(false);
                _player.BodyCollisionShape.Disabled = false;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;
                float t = (float)(_timer / Duration);

                // Gradually hide the labels so they don't suddenly vanish when
                // the animation completes
                Self.FrontLabel.Scale = Vector3.One.Lerp(Vector3.Zero, t);
                Self.BackLabel.Scale = Vector3.One.Lerp(Vector3.Zero, t);

                // Continue the player's trajectory
                _player.LocalVelocity += Vector3.Down * Player.Default.Gravity * (float)delta;
                _playerTrajectoryPos.Origin += _player.Velocity * (float)delta;

                var rot = _player.LocalVelocity.Normalized().ForwardToEulerAnglesRad();
                _player.ModelPitchRad = rot.X;

                // Smoothly transition the player from following the trajectory
                // toward being lerped to the target point
                var lerpPos = _playerStartPos.InterpolateWith(_playerTargetPos, t);
                _player.GlobalTransform = _playerTrajectoryPos.InterpolateWith(lerpPos, t);

                // Move the camera along the path
                _cameraPath.ProgressRatio = t;
                _player.Camera.ManhandledPosition = _cameraPath.GlobalTransform.LookingAt(_player.CameraFocus.GlobalPosition);

                // Go to the loading screen when the player and camera are in
                // position
                if (_timer >= Duration)
                {
                    _player.GlobalTransform = _playerTargetPos;

                    _cameraPath.ProgressRatio = 1;
                    _player.Camera.ManhandledPosition = _cameraPath.GlobalTransform.LookingAt(_player.CameraFocus.GlobalPosition);
                    _player.Camera.GlobalTransform = _player.Camera.ManhandledPosition;
                    _player.Camera.ResetPhysicsInterpolation3D();

                    LevelTransitionManager.Instance.EnterLevel(
                        Self.TargetLevel,
                        Self._skyboxEnvironment
                    );
                }
            }

            private bool IsEnteringFromFront(Vector3 playerVelocity)
            {
                var forward = Self.GlobalForward();
                return playerVelocity.ComponentAlong(forward) < 0;
            }
        }

        private class Exiting : State<Portal>
        {
            private Vector3 _exitAnimationStartPos;
            private double _timer;

            public override void OnStateEntered()
            {
                var player = GetTree().FindNode<Player>();
                _timer = 0;

                // Start the labels hidden.  We'll gradually unhide them as the
                // animation progresses, so they aren't suddenly visible when
                // the level loads in.
                Self.FrontLabel.Scale = Vector3.Zero;
                Self.BackLabel.Scale = Vector3.Zero;

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
                Self.FrontLabel.Scale = Vector3.One;
                Self.BackLabel.Scale = Vector3.One;

                var player = GetTree().FindNode<Player>();
                player.SetVisibleInPortals(false);
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;

                float t = (float)_timer / Self.ExitAnimationDuration;

                // Gradually reveal the labels so they don't suddenly appear
                // when the level loads in
                Self.FrontLabel.Scale = Vector3.Zero.Lerp(Vector3.One, t);
                Self.BackLabel.Scale = Vector3.Zero.Lerp(Vector3.One, t);

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