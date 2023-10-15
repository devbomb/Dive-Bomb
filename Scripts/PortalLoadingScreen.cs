using Godot;

namespace FastDragon
{
    public partial class PortalLoadingScreen : Node3D
    {
        public static readonly float CameraYawRad = Mathf.DegToRad(-145);
        public static readonly float CameraPitchRad = Mathf.DegToRad(45);
        public const float CameraDist = Player.Glide.CameraDistance;

        private const float RestMoveDuration = 2;
        private const float MinLoadingWaitTime = 2;

        private string _levelSceneFile;
        private Vector3 _playerStartRotRad;
        private float _camStartDist;
        private float _camStartYawRad;
        private float _camStartPitchRad;

        private Node3D _playerModel => GetNode<Node3D>("%PlayerModel");
        private AnimationPlayer _playerAnimator => GetNode<AnimationPlayer>("%PlayerAnimator");
        private OrbitCamera _camera => GetNode<OrbitCamera>("%OrbitCamera");

        private WorldEnvironment _worldEnv => GetNode<WorldEnvironment>("%WorldEnv");

        private StateMachine _stateMachine = new StateMachine(typeof(LoadingScreenState));

        public override void _Ready()
        {
            AddChild(_stateMachine);
        }

        public void Initialize(
            string levelSceneFile,
            Environment skyBoxEnvironment,
            double animationStartTime,
            Vector3 playerRotRad,
            float cameraDist,
            float cameraYawRad,
            float cameraPitchRad
        )
        {
            _levelSceneFile = levelSceneFile;
            _worldEnv.Environment = skyBoxEnvironment;
            _playerStartRotRad = playerRotRad;
            _camStartDist = cameraDist;
            _camStartYawRad = cameraYawRad;
            _camStartPitchRad = cameraPitchRad;

            // Start loading the level in the background
            ResourceLoader.LoadThreadedRequest(_levelSceneFile);

            // Sync up the player's animation
            _playerAnimator.Play("PlayerAnimations/Glide");
            _playerAnimator.Seek(animationStartTime, true);

            _camera.ChangeState<OrbitCameraLockedState>();

            // Start the loading screen animation
            _playerModel.GlobalRotation = _playerStartRotRad;
            _camera.OrbitDistance = cameraDist;
            _camera.OrbitYawRad = cameraYawRad;
            _camera.OrbitPitchRad = cameraPitchRad;
            _stateMachine.ChangeState<MovingToRest>();
        }

        private bool DoneLoading()
        {
            var loadStatus = ResourceLoader.LoadThreadedGetStatus(_levelSceneFile);
            return loadStatus == ResourceLoader.ThreadLoadStatus.Loaded;
        }

        private void GoToTargetMap()
        {
            var mapPrefab = (PackedScene)ResourceLoader.LoadThreadedGet(_levelSceneFile);
            GetTree().ChangeSceneToPacked(mapPrefab);
        }

        private abstract partial class LoadingScreenState : State
        {
            protected PortalLoadingScreen _loadingScreen => _stateMachine.GetParent<PortalLoadingScreen>();
            protected OrbitCamera _camera => _loadingScreen._camera;
            protected Node3D _playerModel => _loadingScreen._playerModel;
        }

        private partial class MovingToRest : LoadingScreenState
        {
            private float _timer = 0;

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _timer += delta;

                float t = _timer / RestMoveDuration;

                _playerModel.GlobalRotation = _loadingScreen
                    ._playerStartRotRad
                    .LerpEulerRad(Vector3.Zero, t);

                _camera.OrbitDistance = MathUtils.LerpSinusoidal(
                    _loadingScreen._camStartDist,
                    CameraDist,
                    t
                );

                _camera.OrbitYawRad = MathUtils.LerpAngleSinusoidal(
                    _loadingScreen._camStartYawRad,
                    CameraYawRad,
                    t
                );

                _camera.OrbitPitchRad = MathUtils.LerpAngleSinusoidal(
                    _loadingScreen._camStartPitchRad,
                    CameraPitchRad,
                    t
                );

                if (_timer > RestMoveDuration)
                    ChangeState<WaitingForLoad>();
            }
        }

        private partial class WaitingForLoad : LoadingScreenState
        {
            private float _timer = 0;

            public override void OnStateEntered()
            {
                _playerModel.GlobalRotation = Vector3.Zero;
                _camera.OrbitDistance = CameraDist;
                _camera.OrbitYawRad = CameraYawRad;
                _camera.OrbitPitchRad = CameraPitchRad;

                _timer = MinLoadingWaitTime;
            }

            public override void _Process(double delta)
            {
                _timer -= (float)delta;

                if (_loadingScreen.DoneLoading() && _timer <= 0)
                    _loadingScreen.GoToTargetMap();
            }
        }
    }
}