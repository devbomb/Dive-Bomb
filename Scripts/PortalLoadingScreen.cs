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
        private const float CountingGemsDuration = 1;
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
        private Node3D _loadedScene;
        private int _talliedGems;

        private StateMachine _stateMachine = new StateMachine(typeof(LoadingScreenState));

        public override void _Ready()
        {
            AddChild(_stateMachine);

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

            // Start loading the level in the background
            LoadInBackground(_levelSceneFile);

            // Sync up the player's animation
            _playerAnimator.Play("PlayerAnimations/Glide");
            _playerAnimator.Seek(animationStartTime, true);

            _camera.ChangeState<OrbitCameraLockedState>();

            // Put everything in the starting position
            _playerModel.GlobalRotation = playerStartRotRad;
            _camera.OrbitDistance = cameraStartDist;
            _camera.OrbitYawRad = cameraStartYawRad;
            _camera.OrbitPitchRad = cameraStartPitchRad;

            // Start the animation
            _stateMachine.ChangeState<MovingToRest>();
        }

        private void GoToTargetMap()
        {
            double animationPos = _playerAnimator.CurrentAnimationPosition;
            MapTransitionManager.Instance.ChangeSceneToNode(_loadedScene);

            if (_isReturningHome)
            {
                var portal = GetTargetPortal(_loadedScene);
                portal.PlayExitAnimation(animationPos);
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

        private int CalculateTalliedGems()
        {
            int totalUntallied = 0;

            foreach (var kvp in SaveFile.Current.UntalliedGems)
            {
                GemColor color = kvp.Key;
                int count = kvp.Value;

                totalUntallied += (int)color * count;
            }

            return SaveFile.Current.TotalGemCount - totalUntallied;
        }

        private abstract partial class LoadingScreenState : State
        {
            protected PortalLoadingScreen _screen => _stateMachine.GetParent<PortalLoadingScreen>();
        }

        private partial class MovingToRest : LoadingScreenState
        {
            public override void OnStateEntered()
            {
                GD.Print("Started move-to-rest animation");

                var tween = _screen.CreateTween();
                tween.TweenRotRadSinusoidal(_screen._playerModel, "global_rotation", Vector3.Zero, RestMoveDuration);
                tween.Parallel().TweenProperty(_screen._camera, "OrbitDistance", CameraDist, RestMoveDuration);
                tween.Parallel().TweenAngleRadSinusoidal(_screen._camera, "OrbitYawRad", _screen._cameraYawRad, RestMoveDuration);
                tween.Parallel().TweenAngleRadSinusoidal(_screen._camera, "OrbitPitchRad", _screen._cameraPitchRad, RestMoveDuration);
                tween.TweenCallback(Callable.From(() => ChangeState<CountingGems>()));
            }
        }

        private partial class CountingGems : LoadingScreenState
        {
            private Label _untalliedGemsLabel => _screen.GetNode<Label>("%UntalliedGemsLabel");
            private Label _talliedGemsLabel => _screen.GetNode<Label>("%TalliedGemsLabel");

            private int _untalliedGems;
            private int _talliedGems;
            private double _interval;
            private double _timer;

            public override void OnStateEntered()
            {
                GD.Print("Started counting gems");

                _timer = 0;
                _talliedGems = _screen.CalculateTalliedGems();
                _untalliedGems = SaveFile.Current.TotalGemCount - _talliedGems;
                SaveFile.Current.UntalliedGems.Clear();

                _untalliedGemsLabel.Visible = true;
                _talliedGemsLabel.Visible = true;

                _untalliedGemsLabel.Text = $"Treasure found: {_untalliedGems}";
                _talliedGemsLabel.Text = $"Total treasure: {_talliedGems}";

                _interval = CountingGemsDuration / _untalliedGems;
            }

            public override void _Process(double delta)
            {
                _timer += delta;

                if (_untalliedGems <= 0)
                {
                    ChangeState<WaitingForLoad>();
                    return;
                }

                if (_timer >= _interval)
                {
                    _timer -= _interval;
                    _untalliedGems--;
                    _talliedGems++;

                    _untalliedGemsLabel.Text = $"Treasure found: {_untalliedGems}";
                    _talliedGemsLabel.Text = $"Total treasure: {_talliedGems}";
                }
            }
        }

        private partial class WaitingForLoad : LoadingScreenState
        {
            private double _timer;

            public override void OnStateEntered()
            {
                GD.Print("Started waiting for loading to finish");
                _timer = MinLoadingWaitTime;
            }

            public override void _Process(double delta)
            {
                _timer -= delta;

                if (_timer <= 0 && _screen._loadedScene != null)
                    ChangeState<CorrectingAngle>();
            }
        }

        private partial class CorrectingAngle : LoadingScreenState
        {
            public override void OnStateEntered()
            {
                GD.Print("Started correction animation");

                var tween = _screen.CreateTween();

                TweenSun(tween.Parallel());

                if (_screen._isReturningHome)
                {
                    TweenPlayerToPortal(tween.Parallel());
                }

                tween.TweenCallback(Callable.From(_screen.GoToTargetMap));
            }

            private void TweenSun(Tween tween)
            {
                float duration = CorrectionAnimationDuration;
                var newSun = _screen._loadedScene.FindNode<DirectionalLight3D>();

                tween.TweenRotRadSinusoidal(_screen._oldSun, "rotation", newSun.Rotation, duration);

                var lightProperties = newSun.GetPropertyList()
                    .Select(x => (string)x["name"])
                    .Where(n => n.StartsWith("light_"))
                    .Where(n => n != "light_cull_mask");

                foreach (var propertyName in lightProperties)
                {
                    tween.Parallel().TweenProperty(
                        _screen._oldSun,
                        (string)propertyName,
                        newSun.Get(propertyName),
                        duration
                    );
                }
            }

            void TweenPlayerToPortal(Tween tween)
            {
                float duration = CorrectionAnimationDuration;

                var portal = _screen.GetTargetPortal(_screen._loadedScene);
                Vector3 portalRotRad = GetGlobalTransformOutsideOfTree(portal).Basis.GetEuler();

                tween.TweenRotRadSinusoidal(_screen._playerModel, "global_rotation", portalRotRad, duration);
                tween.Parallel().TweenProperty(_screen._camera, "OrbitDistance", CameraDist, duration);
                tween.Parallel().TweenAngleRadSinusoidal(_screen._camera, "OrbitYawRad", portalRotRad.Y + Mathf.DegToRad(180), duration);
                tween.Parallel().TweenAngleRadSinusoidal(_screen._camera, "OrbitPitchRad", _screen._cameraPitchRad, duration);
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
}