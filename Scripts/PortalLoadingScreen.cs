using System;
using System.Collections.Generic;
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

        [Export] public PackedScene RedGemPrefab;
        [Export] public PackedScene GreenGemPrefab;
        [Export] public PackedScene PurpleGemPrefab;
        [Export] public PackedScene YellowGemPrefab;
        [Export] public PackedScene MagentaGemPrefab;

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

        private const float GemPathBezierControlSpread = 20;
        private const float GemSpawnGrowTime = 0.05f;

        private string _levelSceneFile;
        private string _prevousMapFile;
        private DirectionalLight3D _oldSun;

        private Node3D _playerModel => GetNode<Node3D>("%PlayerModel");
        private AnimationPlayer _playerAnimator => GetNode<AnimationPlayer>("%PlayerAnimator");
        private OrbitCamera _camera => GetNode<OrbitCamera>("%OrbitCamera");

        private WorldEnvironment _worldEnv => GetNode<WorldEnvironment>("%WorldEnv");

        private bool _isReturningHome => _prevousMapFile != null;
        private Node3D _loadedScene;

        private MeshLabel3D _untalliedGemsLabel => GetNode<MeshLabel3D>("%UntalliedGemsLabel");
        private MeshLabel3D _talliedGemsLabel => GetNode<MeshLabel3D>("%TalliedGemsLabel");
        private AnimationPlayer _labelSlider => GetNode<AnimationPlayer>("%LabelSlider");
        private Dictionary<GemColor, int> _untalliedGems;
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
            Godot.Environment skyBoxEnvironment,
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

            // Figure out how many of each gem we'll need to spawn in the
            // gem counting animation later.
            _untalliedGems = SaveFile.Current.UntalliedGems;
            SaveFile.Current.UntalliedGems = new Dictionary<GemColor, int>();
            _talliedGems = SaveFile.Current.TotalGemCount - TotalUntalliedGems();

            // Hide the labels until it's time for them to be seen
            _untalliedGemsLabel.Visible = false;
            _talliedGemsLabel.Visible = false;

            // Start the animation
            _stateMachine.ChangeState<MovingToRest>();
        }

        private int TotalUntalliedGems()
        {
            return _untalliedGems
                .Select(kvp => (int)kvp.Key * kvp.Value)
                .Sum();
        }

        private int TotalIndividualUntalliedGems()
        {
            return _untalliedGems
                .Select(kvp => kvp.Value)
                .Sum();
        }

        private void UpdateLabelText()
        {
            _untalliedGemsLabel.Text = $"Treasure found: {TotalUntalliedGems()}";
            _talliedGemsLabel.Text = $"Total treasure: {_talliedGems}";
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
                tween.TweenCallback(Callable.From(() => ChangeState<SlidingInLabels>()));
            }
        }

        private partial class SlidingInLabels : LoadingScreenState
        {
            public override void OnStateEntered()
            {
                _screen._untalliedGemsLabel.Visible = true;
                _screen._talliedGemsLabel.Visible = true;
                _screen.UpdateLabelText();
                _screen._labelSlider.Play("SlideIn");

                // We just made the text visible, but it won't naturally jump to
                // its starting position until the next frame when the animator
                // processes.  This will result in the text appearing to "blink"
                // for one frame before the animation actually starts.
                //
                // To avoid this, let's just force it to update right now.
                _screen._labelSlider.Seek(0, true);
            }

            public override void _Process(double delta)
            {
                if (!_screen._labelSlider.IsPlaying())
                {
                    if (_screen.TotalUntalliedGems() > 0)
                    {
                        ChangeState<CountingGems>();
                    }
                    else
                    {
                        ChangeState<LettingPlayerReadLabels>();
                    }
                }
            }
        }

        private partial class CountingGems : LoadingScreenState
        {
            private Node3D _gemSpawn => _screen.GetNode<Node3D>("%GemSpawn");
            private Node3D _gemDest => _screen.GetNode<Node3D>("%GemDest");

            private Random _rng = new Random();

            private double _interval;
            private double _timer;

            public override void OnStateEntered()
            {
                GD.Print("Started counting gems");
                _timer = 0;
                _interval = CountingGemsDuration / _screen.TotalIndividualUntalliedGems();
            }

            public override void _Process(double delta)
            {
                _timer += delta;

                if (_screen.TotalUntalliedGems() > 0 && _timer >= _interval)
                {
                    _timer -= _interval;

                    GemColor color = _rng.PickFromWeighted(_screen._untalliedGems);
                    SpawnGem(color);
                }
            }

            private void SpawnGem(GemColor value)
            {
                _screen._untalliedGems[value]--;
                _screen.UpdateLabelText();

                var gem = PrefabForGemColor(value).Instantiate<Node3D>();
                _gemSpawn.GetParent().AddChild(gem);

                gem.Position = _gemSpawn.Position;
                gem.Scale = Vector3.Zero;

                var tween = CreateTween();

                tween.TweenVector3Bezier(
                    gem,
                    "position",
                    _gemDest.Position,
                    RandomBezierControlPoint(GemPathBezierControlSpread),
                    CountingGemsDuration
                );
                tween.Parallel().TweenProperty(gem, "scale", Vector3.One, GemSpawnGrowTime);

                tween.TweenCallback(Callable.From(() => CountGem(gem, value)));
            }

            private void CountGem(Node3D gem, GemColor value)
            {
                _screen._talliedGems += (int)value;
                _screen.UpdateLabelText();

                gem.QueueFree();

                if (_screen._talliedGems >= SaveFile.Current.TotalGemCount)
                    ChangeState<LettingPlayerReadLabels>();
            }

            private Vector3 RandomBezierControlPoint(float spread)
            {
                Vector3 center = _gemDest.Position;
                float offset = (GD.Randf() - 0.5f) * spread;

                return center + (Vector3.Right * offset);
            }

            private PackedScene PrefabForGemColor(GemColor color)
            {
                return color switch
                {
                    GemColor.Red => _screen.RedGemPrefab,
                    GemColor.Green => _screen.GreenGemPrefab,
                    GemColor.Purple => _screen.PurpleGemPrefab,
                    GemColor.Yellow => _screen.YellowGemPrefab,
                    GemColor.Magenta => _screen.MagentaGemPrefab,
                    _ => throw new Exception("Invalid gem color chosen")
                };
            }
        }

        private partial class LettingPlayerReadLabels : LoadingScreenState
        {
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = MinLoadingWaitTime;
            }

            public override void _Process(double delta)
            {
                _timer -= delta;

                if (_timer <= 0)
                {
                    _screen._labelSlider.PlayBackwards("SlideIn");
                    ChangeState<WaitingForLoad>();
                }
            }
        }

        private partial class WaitingForLoad : LoadingScreenState
        {

            public override void _Process(double delta)
            {
                if (!_screen._labelSlider.IsPlaying() && _screen._loadedScene != null)
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