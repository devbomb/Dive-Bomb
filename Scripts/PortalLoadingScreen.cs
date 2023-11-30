using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class PortalLoadingScreen : Node3D
    {
        public static float EnterLevelCameraYawRad => Mathf.DegToRad(-145);
        public static float EnterLevelCameraPitchRad => Mathf.DegToRad(45);

        public static float ReturnHomeCameraYawRad => Mathf.DegToRad(180);
        public static float ReturnHomeCameraPitchRad => 0;

        public const float CameraDist = Player.Glide.CameraDistance;

        private float _cameraYawRad => _isReturningHome
            ? ReturnHomeCameraYawRad
            : EnterLevelCameraYawRad;

        private float _cameraPitchRad => _isReturningHome
            ? ReturnHomeCameraPitchRad
            : EnterLevelCameraPitchRad;

        private const float RestMoveDuration = 2;
        private const float CorrectionAnimationDuration = 1;
        private const float MinLoadingWaitTime = 1;

        private string _levelSceneFile;
        private string _prevousMapFile;
        private DirectionalLight3D _oldSun;

        private Node3D _playerModel => GetNode<Node3D>("%PlayerModel");
        private AnimationPlayer _playerAnimator => GetNode<AnimationPlayer>("%PlayerAnimator");
        private OrbitCamera _camera => GetNode<OrbitCamera>("%OrbitCamera");

        private WorldEnvironment _worldEnv => GetNode<WorldEnvironment>("%WorldEnv");

        private bool _isReturningHome => _prevousMapFile != null;
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
            float cameraStartDist,
            float cameraStartYawRad,
            float cameraStartPitchRad,
            DirectionalLight3D sun
        )
        {
            _levelSceneFile = levelSceneFile;
            _prevousMapFile = previousMapFile;
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
            _camera.OrbitDistance = cameraStartDist;
            _camera.OrbitYawRad = cameraStartYawRad;
            _camera.OrbitPitchRad = cameraStartPitchRad;

            var tween = CreateTween();
            tween.TweenRotRadSinusoidal(_playerModel, "global_rotation", Vector3.Zero, RestMoveDuration);
            tween.Parallel().TweenProperty(_camera, "OrbitDistance", CameraDist, RestMoveDuration);
            tween.Parallel().TweenAngleRadSinusoidal(_camera, "OrbitYawRad", _cameraYawRad, RestMoveDuration);
            tween.Parallel().TweenAngleRadSinusoidal(_camera, "OrbitPitchRad", _cameraPitchRad, RestMoveDuration);
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

            float duration = CorrectionAnimationDuration;
            var tween = CreateTween();

            TweenSun(tween.Parallel());

            if (_isReturningHome)
            {
                TweenPlayerToPortal(tween.Parallel());
            }

            tween.TweenCallback(Callable.From(GoToTargetMap));

            void TweenSun(Tween tween)
            {
                var newSun = _loadedScene.FindNode<DirectionalLight3D>();

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
            }

            void TweenPlayerToPortal(Tween tween)
            {
                var portal = GetTargetPortal(_loadedScene);
                Vector3 portalRotRad = GetGlobalTransformOutsideOfTree(portal).Basis.GetEuler();

                tween.TweenRotRadSinusoidal(_playerModel, "global_rotation", portalRotRad, duration);
                tween.Parallel().TweenProperty(_camera, "OrbitDistance", CameraDist, duration);
                tween.Parallel().TweenAngleRadSinusoidal(_camera, "OrbitYawRad", portalRotRad.Y + Mathf.DegToRad(180), duration);
                tween.Parallel().TweenAngleRadSinusoidal(_camera, "OrbitPitchRad", _cameraPitchRad, duration);
            }
        }

        private void GoToTargetMap()
        {
            MapTransitionManager.Instance.ChangeSceneToNode(_loadedScene);

            if (_isReturningHome)
            {
                var portal = GetTargetPortal(_loadedScene);
                portal.PlayExitAnimation(_playerAnimator.CurrentAnimationPosition);
            }
            else
            {
                _loadedScene.FindNode<Player>().ChangeState<PlayerFlyInState>();
            }
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

        private Transform3D GetGlobalTransformOutsideOfTree(Node3D node)
        {
            var parent = GetParentOrNull<Node3D>();
            if (parent == null)
            {
                return node.Transform;
            }

            return node.Transform * GetGlobalTransformOutsideOfTree(parent);
        }
    }
}