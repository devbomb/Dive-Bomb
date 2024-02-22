using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class PortalLoadingScreen : Node3D
    {
        public static float EnterLevelCameraYawRad => Mathf.DegToRad(-180);
        public static float EnterLevelCameraPitchRad => Mathf.DegToRad(60);

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
        private const float SkipDuration = 0.25f;
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
            string animationName,
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

            // Sync up the player's animation...
            _playerAnimator.Play(animationName);
            _playerAnimator.Seek(animationStartTime, true);

            // ...and then make it transition the animation used for loading
            if (!_isReturningHome)
            {
                _playerAnimator.Play("Glide", 0.25f);
            }
            else
            {
                _playerAnimator.Play("Levitate", 2);
            }

            _camera.DisableInput = true;

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

            var realPlayer = _loadedScene.FindNode<Player>();
            realPlayer.Animator.Play(_playerAnimator.AssignedAnimation, 0);
            realPlayer.Animator.Seek(_playerAnimator.CurrentAnimationPosition, true);
            realPlayer.Animator.Advance(0);

            if (_isReturningHome)
            {
                var portal = GetTargetPortal(_loadedScene);
                portal.PlayExitAnimation();
            }
            else
            {
                realPlayer.ChangeState<PlayerFlyInState>();
            }
        }

        private void LoadInBackground(string sceneFilePath)
        {
            var thread = new System.Threading.Thread(() =>
            {
                var prefab = ResourceLoader.Load<PackedScene>(sceneFilePath);
                var node = prefab.Instantiate<Node3D>();
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
            public virtual bool Skippable => false;
            protected PortalLoadingScreen _screen => _stateMachine.GetParent<PortalLoadingScreen>();

            public override void _Input(InputEvent ev)
            {
                bool skipPressed =
                    InputService.PauseJustPressed(ev) ||
                    InputService.JumpJustPressed(ev);

                if (skipPressed && Skippable)
                {
                    ChangeState<Skipping>();
                }
            }
        }

        private partial class Skipping : LoadingScreenState
        {
            public override void OnStateEntered()
            {
                GD.Print("Skipping animations");

                var tween = _screen.CreateTween();
                tween.TweenRotRadSinusoidal(_screen._playerModel, "global_rotation", Vector3.Zero, SkipDuration);
                tween.Parallel().TweenProperty(_screen._camera, "OrbitDistance", CameraDist, SkipDuration);
                tween.Parallel().TweenAngleRadSinusoidal(_screen._camera, "OrbitYawRad", _screen._cameraYawRad, SkipDuration);
                tween.Parallel().TweenAngleRadSinusoidal(_screen._camera, "OrbitPitchRad", _screen._cameraPitchRad, SkipDuration);
                tween.TweenCallback(Callable.From(() => ChangeState<LettingPlayerReadLabels>()));
            }
        }

        private partial class MovingToRest : LoadingScreenState
        {
            public override bool Skippable => true;

            private Tween _tween;

            public override void OnStateEntered()
            {
                GD.Print("Started move-to-rest animation");

                _tween = _screen.CreateTween();
                _tween.TweenRotRadSinusoidal(_screen._playerModel, "global_rotation", Vector3.Zero, RestMoveDuration);
                _tween.Parallel().TweenProperty(_screen._camera, "OrbitDistance", CameraDist, RestMoveDuration);
                _tween.Parallel().TweenAngleRadSinusoidal(_screen._camera, "OrbitYawRad", _screen._cameraYawRad, RestMoveDuration);
                _tween.Parallel().TweenAngleRadSinusoidal(_screen._camera, "OrbitPitchRad", _screen._cameraPitchRad, RestMoveDuration);
                _tween.TweenCallback(Callable.From(() => ChangeState<SlidingInLabels>()));
            }

            public override void OnStateExited()
            {
                _tween.Stop();
            }
        }

        private partial class SlidingInLabels : LoadingScreenState
        {
            public override bool Skippable => true;

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
            public override bool Skippable => true;

            private Node3D _gemSpawn => _screen.GetNode<Node3D>("%GemSpawn");
            private Node3D _gemDest => _screen.GetNode<Node3D>("%GemDest");

            private Random _rng = new Random();

            private double _interval;
            private double _timer;
            private Node3D _gemHolder;

            public override void OnStateEntered()
            {
                GD.Print("Started counting gems");
                _timer = 0;
                _interval = CountingGemsDuration / _screen.TotalIndividualUntalliedGems();

                _gemHolder = new Node3D();
                _gemSpawn.GetParent().AddChild(_gemHolder);
            }

            public override void OnStateExited()
            {
                _gemHolder.QueueFree();
                _gemHolder = null;
            }

            private void OnGemCounted(int value)
            {
                _screen._talliedGems += value;
                _screen.UpdateLabelText();

                if (_screen._talliedGems >= SaveFile.Current.TotalGemCount)
                    ChangeState<LettingPlayerReadLabels>();
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

                var gem = new CountableGem(
                    value,
                    _gemSpawn.Position,
                    _gemDest.Position,
                    RandomBezierControlPoint(GemPathBezierControlSpread)
                );
                _gemHolder.AddChild(gem);

                var model = ModelPrefabForGemColor(value).Instantiate<Node3D>();
                gem.AddChild(model);

                gem.Counted += OnGemCounted;
            }

            private Vector3 RandomBezierControlPoint(float spread)
            {
                Vector3 center = _gemDest.Position;
                float offset = (GD.Randf() - 0.5f) * spread;

                return center + (Vector3.Right * offset);
            }

            private PackedScene ModelPrefabForGemColor(GemColor color)
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

            private partial class CountableGem : Node3D
            {
                [Signal] public delegate void CountedEventHandler(int value);

                private readonly GemColor _value;
                private readonly Vector3 _start;
                private readonly Vector3 _end;
                private readonly Vector3 _control;

                private float _timer = 0;

                public CountableGem(
                    GemColor value,
                    Vector3 start,
                    Vector3 end,
                    Vector3 control
                )
                {
                    _value = value;
                    _start = start;
                    _end = end;
                    _control = control;

                    Position = _start;
                    Scale = Vector3.Zero;
                }

                public override void _Process(double deltaD)
                {
                    float delta = (float)deltaD;

                    _timer += delta;

                    float positionT = Mathf.Min(1, _timer / CountingGemsDuration);
                    float scaleT = Mathf.Min(1, _timer / GemSpawnGrowTime);

                    Position = _start.LerpBezier(_end, _control, positionT);
                    Scale = Vector3.Zero.Lerp(Vector3.One, scaleT);

                    if (_timer >= CountingGemsDuration)
                    {
                        EmitSignal(SignalName.Counted, (int)_value);
                        QueueFree();
                    }
                }
            }
        }

        private partial class LettingPlayerReadLabels : LoadingScreenState
        {
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = MinLoadingWaitTime;
                _screen._labelSlider.Play("RESET");
                _screen._labelSlider.Advance(0);
                _screen._untalliedGemsLabel.Visible = true;
                _screen._talliedGemsLabel.Visible = true;

                _screen._untalliedGems.Clear();
                _screen._talliedGems = SaveFile.Current.TotalGemCount;
                _screen.UpdateLabelText();
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
                if (newSun == null)
                    return;

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