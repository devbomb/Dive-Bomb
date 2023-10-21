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

        private Node3D _playerModel => GetNode<Node3D>("%PlayerModel");
        private AnimationPlayer _playerAnimator => GetNode<AnimationPlayer>("%PlayerAnimator");
        private OrbitCamera _camera => GetNode<OrbitCamera>("%OrbitCamera");

        private WorldEnvironment _worldEnv => GetNode<WorldEnvironment>("%WorldEnv");

        private bool _animationDone;

        public void Initialize(
            string levelSceneFile,
            Environment skyBoxEnvironment,
            double animationStartTime,
            Vector3 playerStartRotRad,
            float cameraDist,
            float cameraYawRad,
            float cameraPitchRad
        )
        {
            _levelSceneFile = levelSceneFile;
            _worldEnv.Environment = skyBoxEnvironment;

            // Start loading the level in the background
            ResourceLoader.LoadThreadedRequest(_levelSceneFile);

            // Sync up the player's animation
            _playerAnimator.Play("PlayerAnimations/Glide");
            _playerAnimator.Seek(animationStartTime, true);

            _camera.ChangeState<OrbitCameraLockedState>();

            // Start the loading screen animation
            _playerModel.GlobalRotation = playerStartRotRad;
            _camera.OrbitDistance = cameraDist;
            _camera.OrbitYawRad = cameraYawRad;
            _camera.OrbitPitchRad = cameraPitchRad;
            _animationDone = false;

            var tween = CreateTween();
            tween.TweenProperty(_playerModel, "global_rotation", Vector3.Zero, RestMoveDuration);
            tween.Parallel().TweenProperty(_camera, "OrbitDistance", CameraDist, RestMoveDuration);
            tween.Parallel().TweenAngleRadSinusoidal(_camera, "OrbitYawRad", CameraYawRad, RestMoveDuration);
            tween.Parallel().TweenAngleRadSinusoidal(_camera, "OrbitPitchRad", CameraPitchRad, RestMoveDuration);
            tween.TweenInterval(MinLoadingWaitTime);
            tween.TweenCallback(Callable.From(() => _animationDone = true));
        }

        public override void _Process(double delta)
        {
            if (DoneLoading() && _animationDone)
                GoToTargetMap();
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
    }
}