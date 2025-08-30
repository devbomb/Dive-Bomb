using Godot;

namespace FastDragon
{
    public partial class PortalSurface : Area3D
    {
        [Export(PropertyHint.File)] public string SkyboxEnvironment;
        [Export(PropertyHint.File)] public string TargetLevel;

        private Camera3D _portalCamera => GetNode<Camera3D>("%PortalCamera");

        private MeshInstance3D _portalMaterialHolder => GetNode<MeshInstance3D>("%PortalMaterialHolder");

        private RayCast3D _normalDetector => GetNode<RayCast3D>("%NormalDetector");

        private Area3D _cameraDetector => GetNode<Area3D>("%CameraDetector");
        private CollisionShape3D _cameraDetectorPlane => GetNode<CollisionShape3D>("%CameraDetectorPlane");

        private StateMachine _stateMachine = new StateMachine();
        private Player _player;
        private Vector3 _playerTargetRotRad;

        public void SetSkybox(string skyboxEnvironment)
        {
            _portalCamera.Environment = ResourceLoader.Load<Environment>(skyboxEnvironment);
        }

        public override void _Ready()
        {
            AddChild(_stateMachine);
            _stateMachine.ChangeState<Idle>();

            BodyEntered += OnBodyEntered;

            if (ResourceLoader.Exists(SkyboxEnvironment))
                _portalCamera.Environment = ResourceLoader.Load<Environment>(SkyboxEnvironment);

            foreach (var mesh in this.EnumerateDescendantsOfType<MeshInstance3D>())
            {
                if (mesh != _portalMaterialHolder)
                    mesh.MaterialOverride = _portalMaterialHolder.MaterialOverride;
            }
        }

        private void OnBodyEntered(Node3D body)
        {
            if (body is Player player && !(player.CurrentState is PlayerManhandledState))
            {
                _player = player;
                UpdatePlayerTargetRot(player);

                if (player.IsOnFloor())
                    _stateMachine.ChangeState<Jumping>();
                else
                    _stateMachine.ChangeState<Flying>();
            }
        }

        private abstract partial class PortalState : State<PortalSurface>
        {
            protected Player Player => Self._player;
        }

        private class Idle : PortalState {}

        private class Jumping : PortalState
        {
            public override void OnStateEntered()
            {
                Player.SetVisibleInPortals(true);
                Player.ChangeState<PlayerManhandledState>();
                Player.Animator.Play("Jump");
                Player.Velocity = Vector3.Up * Player.Jump.InitVSpeed;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                Player.Velocity += Vector3.Down * Player.Default.Gravity * delta;
                Player.GlobalPosition += Player.Velocity * delta;

                Self.RotatePlayer(delta);
                Self.RecenterCamera(delta);

                if (Player.Velocity.Y <= 0)
                    ChangeState<Flying>();
            }
        }

        private class Flying : PortalState
        {
            public override void OnStateEntered()
            {
                Player.SetVisibleInPortals(true);
                Player.ChangeState<PlayerManhandledState>();
                Player.Animator.Play("Dive");
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                if (CameraIsBehindPlayer())
                    Player.GlobalPosition += Player.GlobalForward() * Player.Glide.MaxFSpeed * delta;

                Self.RotatePlayer(delta);
                Self.RecenterCamera(delta);

                if (CameraIsTouchingPortal() && CameraIsBehindPlayer())
                {
                    LevelTransitionManager.Instance.EnterLevel(
                        Self.TargetLevel,
                        Self._portalCamera.Environment
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
                    Player.Camera.OrbitYawRad
                );

                return Mathf.Abs(angleDiffRad) < Mathf.DegToRad(45);
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

            _player.Camera.OrbitPitchRad = AngleMath.DecayToward(
                _player.Camera.OrbitPitchRad,
                0,
                5,
                delta
            );
        }
    }
}