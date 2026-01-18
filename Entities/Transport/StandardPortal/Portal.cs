using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class Portal : Node3D
    {
        [Export(PropertyHint.FilePath)] public string SkyboxEnvironment;
        [Export(PropertyHint.FilePath)] public string TargetLevel;
        [Export] public string Text;

        [Export(PropertyHint.Enum, "Level,Boss,PostBoss,Bonus")]
        public PortalType Type;
        public enum PortalType
        {
            /// <summary>
            /// The player is required to reach this level's exit for the
            /// <see cref="PortalType.Boss"/> portal to unlock
            /// </summary>
            Level = 0,

            /// <summary>
            /// This portal cannot be used until the player has reached the exit
            /// in all of this hub world's <see cref="PortalType.Level"/>s.
            /// </summary>
            Boss = 1,

            /// <summary>
            /// This portal cannot be used until the player has reached the exit
            /// in this hub world's <see cref="PortalType.Boss"/>.
            /// </summary>
            PostBoss = 2,

            /// <summary>
            /// This portal does not lead to any required levels, and no other
            /// levels are required to use it.
            /// </summary>
            Bonus = 3,
        }

        [Export] public float ExitAnimationDuration = 2.5f;
        [Export] public float ExitAnimationStartHeight = 0;
        [Export] public float ExitAnimationParabolaHeight = 2;
        [Export] public float ExitAnimationCameraRotateDuration = 1f;

        [Export] public Node3D PlayerSpawn;

        [ExportGroup("Internal")]
        [Export] public PortalSurface PortalSurface;
        [Export] public TextureRect FullScreenPortalCamTexture;

        [ExportSubgroup("Labels")]
        [Export] public MeshInstance3D FrontLabel;
        [Export] public MeshInstance3D BackLabel;
        [Export] public AnimationPlayer LabelAnimator;

        [ExportSubgroup("Front")]
        [Export] public Node3D PlayerEnterFrontPoint;
        [Export] public PathFollow3D CameraEnterFrontPath;

        [ExportSubgroup("Back")]
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

            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            GetTree().FindNode<Player>().SetVisibleInPortals(false);
            _stateMachine.ChangeState<Idle>();
        }

        public bool IsUnlocked()
        {
            switch (Type)
            {
                case PortalType.Boss: return CompletedAllLevelsOfType(PortalType.Level);
                case PortalType.PostBoss: return CompletedAllLevelsOfType(PortalType.Boss);
                default: return true;
            }

            bool CompletedAllLevelsOfType(PortalType type)
            {
                return GetTree().Root
                    .EnumerateDescendantsOfType<Portal>()
                    .Where(p => p.Type == type)
                    .All(p => SaveFileManager.Current.LevelExitReached(p.TargetLevel));
            }
        }

        public void OnBodyEntered(Node3D body)
        {
            if (body is Player player && !(player.CurrentState is PlayerManhandledState) && IsUnlocked())
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

        public void ShowLabels()
        {
            // Generating TextMesh text is relatively CPU-intensive; if every
            // portal were to generate its text all at once at the start of the
            // level, it would cause a noticeable hitch, which would spoil the
            // smooth transition illusion.
            //
            // Therefore, we defer generating the mesh until the player comes
            // within some range of the portal(detected via an Area3D placed in
            // the editor, hence why it looks like nothing calls this method).
            // That ensures at most one portal is generating text on frame 1 of
            // the level, keeping the hitch short.
            LabelAnimator.Play("Appear");

            // FrontLabel and BackLabel have the same non-unique(but still
            // scene-local) TextMesh assigned to them in the editor, so we only
            // need to modify one of them to update both of them.
            var textMesh = (TextMesh)FrontLabel.Mesh;
            if (textMesh.Text != Text)
                textMesh.Text = Text;
        }

        public void HideLabels()
        {
            LabelAnimator.Play("Disappear");
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
                Self.HideLabels();

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
                _player.SetVisibleInPortals(false);
                _player.BodyCollisionShape.Disabled = false;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;
                float t = (float)(_timer / Duration);

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

                // Warp the player to the start pos of the animation
                _exitAnimationStartPos = Self.GlobalPosition;
                _exitAnimationStartPos -= Self.GlobalForward() * (player.Camera.OrbitDistance - 1);
                _exitAnimationStartPos += Vector3.Up * Self.ExitAnimationStartHeight;

                player.SetVisibleInPortals(true);
                player.ChangeState<PlayerManhandledState>();
                player.GlobalRotation = Self.PlayerSpawn.GlobalRotation;
                player.GlobalPosition = _exitAnimationStartPos;
                player.ResetPhysicsInterpolation3D();

                player.CameraFocus.Reset();

                player.Camera.IgnoreObstructions = true;
                player.Camera.OrbitYawRad = Self.PlayerSpawn.GlobalRotation.Y + Mathf.DegToRad(180);
                player.Camera.OrbitPitchRad = 0;
                player.Camera.ApplyAnglesAndDistance();
                player.Camera.ResetPhysicsInterpolation3D();

                // HACK: For whatever reason, portal surface textures don't
                // become visible immediately when the level loads.  To prevent
                // the player from briefly seeing behind the portal, temporarily
                // make the portal cam's texture occupy the entire screen.
                Self.PortalSurface.PortalCamera.GlobalTransform = player.Camera.GlobalTransform;
                Self.FullScreenPortalCamTexture.Texture = Self.PortalSurface.SubViewport.GetTexture();
                Self.FullScreenPortalCamTexture.Visible = true;
            }

            public override void _Process(double delta)
            {
                // HACK: Hide the portal cam screen overlay after we know
                // the portal texture is visible
                Self.FullScreenPortalCamTexture.Visible = false;
            }

            public override void OnStateExited()
            {
                Self.FullScreenPortalCamTexture.Visible = false;

                var player = GetTree().FindNode<Player>();
                player.SetVisibleInPortals(false);
                player.Camera.IgnoreObstructions = false;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;

                MovePlayer();
                RotateCamera();

                if (_timer > Self.ExitAnimationDuration)
                {
                    var player = GetTree().FindNode<Player>();
                    player.GlobalPosition = Self.PlayerSpawn.GlobalPosition;
                    player.ResetPhysicsInterpolation3D();
                    player.ChangeState<PlayerStandState>();

                    ChangeState<Idle>();
                }
            }

            private void MovePlayer()
            {
                float t = (float)_timer / Self.ExitAnimationDuration;

                // Move the player
                var player = GetTree().FindNode<Player>();
                player.GlobalPosition = _exitAnimationStartPos.LerpParabola(
                    Self.PlayerSpawn.GlobalPosition,
                    Self.ExitAnimationParabolaHeight,
                    t
                );
            }

            private void RotateCamera()
            {
                // Rotate the camera behind the player, since they probably
                // don't want to jump right back into the level they just came
                // from.
                float duration = Self.ExitAnimationCameraRotateDuration;
                float startTime = Self.ExitAnimationDuration - duration;

                float t = (float)(_timer - startTime) / duration;
                t = Mathf.Clamp(t, 0, 1);
                t = Mathf.SmoothStep(0, 1, t);

                float cameraStartYawRad = Self.PlayerSpawn.GlobalRotation.Y + Mathf.DegToRad(180);
                float cameraEndYawRad =  Self.PlayerSpawn.GlobalRotation.Y;
                var player = GetTree().FindNode<Player>();
                player.Camera.OrbitYawRad = Mathf.LerpAngle(cameraStartYawRad, cameraEndYawRad, t);

                // All the camera to be obstructed by the portal after it starts
                // rotating.  Otherwise, the camera will end up behind the portal.
                player.Camera.IgnoreObstructions = _timer < startTime;
            }
        }

    }
}