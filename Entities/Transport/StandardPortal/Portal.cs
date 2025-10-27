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

        [ExportGroup("Internal")]
        [Export] public PortalSurface PortalSurface;
        [Export] public RayCast3D _normalDetector;
        [Export] public Area3D _cameraDetector;

        private MeshLabel3D _frontLabel => GetNode<MeshLabel3D>("%FrontLabel");
        private MeshLabel3D _backLabel => GetNode<MeshLabel3D>("%BackLabel");

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

            _frontLabel.Text = Text;
            _backLabel.Text = Text;

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
            UpdatePlayerTargetRot(player);

            if (player.IsOnFloor())
                _stateMachine.ChangeState<Jumping>();
            else
                _stateMachine.ChangeState<Flying>();
        }

        public void PlayExitAnimation()
        {
            _stateMachine.ChangeState<Exiting>();
        }

        private class Idle : State<Portal>
        {
        }

        private class Jumping : State<Portal>
        {
            private Player _player;

            public override void OnStateEntered()
            {
                _player = GetTree().FindNode<Player>();

                _player.SetVisibleInPortals(true);
                _player.ChangeState<PlayerManhandledState>();
                _player.Animator.Play("Jump");
                _player.Velocity = Vector3.Up * Player.Jump.InitVSpeed;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _player.Velocity += Vector3.Down * Player.Default.Gravity * delta;
                _player.GlobalPosition += _player.Velocity * delta;

                Self.RotatePlayer(_player,  delta);
                Self.RecenterCamera(_player, delta);

                if (_player.Velocity.Y <= 0)
                    ChangeState<Flying>();
            }
        }

        private class Flying : State<Portal>
        {
            private Player _player;

            public override void OnStateEntered()
            {
                _player = GetTree().FindNode<Player>();

                _player.SetVisibleInPortals(true);
                _player.ChangeState<PlayerManhandledState>();
                _player.Animator.Play("Dive");
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                if (CameraIsBehindPlayer())
                    _player.GlobalPosition += _player.GlobalForward() * Player.Glide.MaxFSpeed * delta;

                Self.RotatePlayer(_player, delta);
                Self.RecenterCamera(_player, delta);

                if (CameraIsTouchingPortal() && CameraIsBehindPlayer())
                {
                    LevelTransitionManager.Instance.EnterLevel(
                        Self.TargetLevel,
                        Self._skyboxEnvironment
                    );
                }
            }

            private bool CameraIsTouchingPortal()
            {
                foreach (var area in Self._cameraDetector.GetOverlappingAreas())
                {
                    if (area.IsInGroup("CameraArea"))
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool CameraIsBehindPlayer()
            {
                float angleDiffRad = Mathf.AngleDifference(
                    Self._playerTargetRotRad.Y,
                    _player.Camera.OrbitYawRad
                );

                return Mathf.Abs(angleDiffRad) < Mathf.DegToRad(45);
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

        private void UpdatePlayerTargetRot(Player player)
        {
            // Use a raycast to find what the collision with the player would
            // be, if the portal were solid
            _normalDetector.GlobalPosition = player.GlobalPosition;
            _normalDetector.TargetPosition = GlobalPosition - _normalDetector.GlobalPosition;
            _normalDetector.GlobalRotation = Vector3.Zero;
            _normalDetector.ForceUpdateTransform();
            _normalDetector.ForceRaycastUpdate();

            // Decide which way to rotate the player when they start flying
            Vector3 forwardDir = _normalDetector.GetCollisionNormal();
            Vector3 forwardRad = forwardDir.ForwardToEulerAnglesRad();
            Vector3 backwardRad = forwardRad + (Vector3.Up * Mathf.DegToRad(180));
            float angleToPlayerRad = (GlobalPosition - player.GlobalPosition)
                .ForwardToEulerAnglesRad()
                .Y;

            float diffToForwardRad = AngleMath.Difference(angleToPlayerRad, forwardRad.Y);
            float diffToBackwardRad = AngleMath.Difference(angleToPlayerRad, backwardRad.Y);

            bool isForward = Mathf.Abs(diffToForwardRad) < Mathf.Abs(diffToBackwardRad);

            _playerTargetRotRad = isForward
                ? forwardRad
                : backwardRad;

            // Orient the camera detector plane to be perpendicular to the
            // normal
            _cameraDetector.GlobalPosition = _normalDetector.GetCollisionPoint();
            _cameraDetector.GlobalRotation = isForward
                ? backwardRad
                : forwardRad;
        }

        private void RotatePlayer(Player player, float delta)
        {
            player.GlobalRotation = player.GlobalRotation.RotateTowardEulerRad(
                _playerTargetRotRad,
                delta * Mathf.DegToRad(180)
            );
        }

        private void RecenterCamera(Player player, float delta)
        {
            player.Camera.OrbitYawRad = AngleMath.DecayToward(
                player.Camera.OrbitYawRad,
                _playerTargetRotRad.Y,
                5,
                delta
            );

            player.Camera.OrbitPitchRad = AngleMath.DecayToward(
                player.Camera.OrbitPitchRad,
                0,
                5,
                delta
            );
        }
    }
}