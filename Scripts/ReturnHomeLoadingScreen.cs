using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class ReturnHomeLoadingScreen : Node3D
    {
        public static readonly float CameraYawRad = Mathf.DegToRad(180);
        public static readonly float CameraPitchRad = 0;
        public const float CameraDist = Player.Glide.CameraDistance;

        private const float RestMoveDuration = 2;
        private const float CorrectionAnimationDuration = 1;
        private const float MinLoadingWaitTime = 1;

        private string _levelSceneFile;
        private string _prevousMapFile;

        private Node3D _playerModel => GetNode<Node3D>("%PlayerModel");
        private AnimationPlayer _playerAnimator => GetNode<AnimationPlayer>("%PlayerAnimator");
        private OrbitCamera _camera => GetNode<OrbitCamera>("%OrbitCamera");

        private WorldEnvironment _worldEnv => GetNode<WorldEnvironment>("%WorldEnv");

        private bool _animationDone;
        private bool _startedCorrectionAnimation;
        private Node3D _loadedScene;

        public override void _Ready()
        {
            // Allow the player to be seen on top of the black fade curtain
            var visuals = this.EnumerateDescendantsOfType<VisualInstance3D>();
            foreach (var v in visuals)
            {
                v.SetLayerMaskValue(RenderLayer.VisibleInPortals, true);
            }
        }

        public void Initialize(
            string levelSceneFile,
            string previousMapFile,
            Environment skyBoxEnvironment,
            double animationStartTime,
            Vector3 playerStartRotRad,
            float cameraDist,
            float cameraYawRad,
            float cameraPitchRad
        )
        {
            _levelSceneFile = levelSceneFile;
            _prevousMapFile = previousMapFile;
            _worldEnv.Environment = skyBoxEnvironment;

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

            // HACK: temporarily add the level to the tree, so we can
            // get the portal's global transform
            GetTree().Root.AddChild(_loadedScene);
            Vector3 portalRotRad = GetTargetPortal(_loadedScene).GlobalRotation;
            GetTree().Root.RemoveChild(_loadedScene);

            float duration = CorrectionAnimationDuration;
            var tween = CreateTween();
            tween.TweenRotRadSinusoidal(_playerModel, "global_rotation", portalRotRad, duration);
            tween.Parallel().TweenProperty(_camera, "OrbitDistance", CameraDist, duration);
            tween.Parallel().TweenAngleRadSinusoidal(_camera, "OrbitYawRad", -portalRotRad.Y, duration);
            tween.Parallel().TweenAngleRadSinusoidal(_camera, "OrbitPitchRad", CameraPitchRad, duration);
            tween.TweenCallback(Callable.From(GoToTargetMap));
        }

        private void GoToTargetMap()
        {
            MapTransitionManager.Instance.ChangeSceneToNode(_loadedScene);

            var portal = GetTargetPortal(_loadedScene);
            portal.PlayExitAnimation(_playerAnimator.CurrentAnimationPosition);
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

        private Portal GetTargetPortal(Node sceneRoot)
        {
            return sceneRoot
                .EnumerateDescendantsOfType<Portal>()
                .First(p => p.TargetMap == _prevousMapFile);

        }
    }
}