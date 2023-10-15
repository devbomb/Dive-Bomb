using Godot;

namespace FastDragon
{
    public partial class PortalLoadingScreen : Node3D
    {
        private string _levelSceneFile;
        private bool _animationDone = false;

        private Node3D _playerModel => GetNode<Node3D>("%PlayerModel");
        private AnimationPlayer _playerAnimator => GetNode<AnimationPlayer>("%PlayerAnimator");
        private OrbitCamera _camera => GetNode<OrbitCamera>("%OrbitCamera");

        private WorldEnvironment _worldEnv => GetNode<WorldEnvironment>("%WorldEnv");

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

            // Start loading the level in the background
            ResourceLoader.LoadThreadedRequest(_levelSceneFile);

            // Sync up the player's animation
            _playerAnimator.Play("PlayerAnimations/Glide");
            _playerAnimator.Seek(animationStartTime, true);

            _playerModel.Rotation = playerRotRad;

            _camera.ChangeState<OrbitCameraLockedState>();
            _camera.OrbitDistance = cameraDist;
            _camera.OrbitYawRad = cameraYawRad;
            _camera.OrbitPitchRad = cameraPitchRad;

            // TODO: Play a real animation instead of this lame pause
            _animationDone = false;
            GetTree().CreateTimer(2).Timeout += () => _animationDone = true;
        }

        public override void _Process(double deltaD)
        {
            var loadStatus = ResourceLoader.LoadThreadedGetStatus(_levelSceneFile);
            bool doneLoading = loadStatus == ResourceLoader.ThreadLoadStatus.Loaded;

            if (_animationDone && doneLoading)
            {
                var mapPrefab = (PackedScene)ResourceLoader.LoadThreadedGet(_levelSceneFile);
                GetTree().ChangeSceneToPacked(mapPrefab);
            }
        }
    }
}