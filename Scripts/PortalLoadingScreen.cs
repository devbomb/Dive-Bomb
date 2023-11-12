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
        private const float CorrectionAnimationDuration = 1;
        private const float MinLoadingWaitTime = 2;

        private string _levelSceneFile;
        private DirectionalLight3D _oldSun;

        private Node3D _playerModel => GetNode<Node3D>("%PlayerModel");
        private AnimationPlayer _playerAnimator => GetNode<AnimationPlayer>("%PlayerAnimator");
        private OrbitCamera _camera => GetNode<OrbitCamera>("%OrbitCamera");

        private WorldEnvironment _worldEnv => GetNode<WorldEnvironment>("%WorldEnv");

        private bool _animationDone;
        private bool _startedCorrectionAnimation;
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
            _oldSun = sun;
            _oldSun.SkyMode = DirectionalLight3D.SkyModeEnum.LightOnly;
            AddChild(_oldSun);

            _loadedScene = null;
            _animationDone = false;
            _startedCorrectionAnimation = false;

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
            if (_loadedScene != null && _animationDone && !_startedCorrectionAnimation)
                StartCorrectionAnimation();
        }

        private void StartCorrectionAnimation()
        {
            _startedCorrectionAnimation = true;

            // Cross fade between the old sun and the new sun
            var newSun = _loadedScene.FindNode<DirectionalLight3D>();

            float duration = CorrectionAnimationDuration;
            var tween = CreateTween();
            tween.TweenRotRadSinusoidal(_oldSun, "rotation", newSun.Rotation, duration);

            var lightProperties = newSun.GetPropertyList()
                .Select(x => (string)x["name"])
                .Where(n => n.StartsWith("light_"))
                .Where(n => n != "light_cull_mask");

            foreach (var propertyName in lightProperties)
            {
                tween.Parallel().TweenProperty(
                    _oldSun,
                    (string)propertyName,
                    newSun.Get(propertyName),
                    duration
                );
            }

            tween.TweenCallback(Callable.From(GoToTargetMap));
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