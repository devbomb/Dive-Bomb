using System.Linq;
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
            float cameraPitchRad,
            DirectionalLight3D sun
        )
        {
            _levelSceneFile = levelSceneFile;
            _worldEnv.Environment = skyBoxEnvironment;
            AddChild(sun);

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
            tween.TweenRotRadSinusoidal(_playerModel, "global_rotation", Vector3.Zero, RestMoveDuration);
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
            MapTransitionManager.Instance.ChangeSceneToNode(_loadedScene);
            _loadedScene.FindNode<Player>().ChangeState<PlayerFlyInState>();
        }

        private void LoadInBackground(string sceneFilePath)
        {
            var thread = new System.Threading.Thread(() =>
            {
                var prefab = ResourceLoader.Load<PackedScene>(sceneFilePath);
                var node = prefab.Instantiate<Node3D>();

                // HACK: If this is a trenchbroom scene, build the meshes in
                // this thread instead of the main thread.  After all, the level
                // isn't _truly_ loaded until the meshes are built.
                if (node.HasMethod("_refresh"))
                {
                    node.Set("_hackityHackHackDisableRefresh", true);
                    node.Call("build_meshes");
                }

                _loadedScene = node;
            });

            thread.Start();
        }
    }
}