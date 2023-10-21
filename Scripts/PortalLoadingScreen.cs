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
        private Node3D _loadedScene;

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

            _loadedScene = null;
            _animationDone = false;

            // Start loading the level in the background
            LoadInBackground(_levelSceneFile);

            // Sync up the player's animation
            _playerAnimator.Play("PlayerAnimations/Glide");
            _playerAnimator.Seek(animationStartTime, true);

            _camera.ChangeState<OrbitCameraLockedState>();

            // Start the loading screen animation
            _playerModel.GlobalRotation = playerStartRotRad;
            _camera.OrbitDistance = cameraDist;
            _camera.OrbitYawRad = cameraYawRad;
            _camera.OrbitPitchRad = cameraPitchRad;

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
            if (_loadedScene != null && _animationDone)
                GoToTargetMap();
        }

        private void DoneLoading(Node3D loadedScene)
        {
            _loadedScene = loadedScene;
        }

        private void GoToTargetMap()
        {
            var oldScene = GetTree().CurrentScene;
            oldScene.GetParent().RemoveChild(oldScene);
            oldScene.QueueFree();

            GetTree().Root.AddChild(_loadedScene);
            GetTree().CurrentScene = _loadedScene;
        }

        private void LoadInBackground(string sceneFilePath)
        {
            var thread = new System.Threading.Thread(() =>
            {
                var prefab = ResourceLoader.Load<PackedScene>(sceneFilePath);
                var node = prefab.Instantiate<Node3D>();

                // TODO: If this is a trenchbroom scene, refresh it first.

                _loadedScene = node;
            });

            thread.Start();
        }
    }
}