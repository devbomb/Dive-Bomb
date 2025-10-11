using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class MissionStatsScreen : Node3D
    {
        [Export(PropertyHint.FilePath)] public string PreviousLevelScenePath;
        [Export(PropertyHint.FilePath)] public string HomeWorldScenePath;

        [Export] public MeshInstance3D Backdrop;
        [Export] public Node3D PlayerModel;
        [Export] public Camera3D Camera;
        [Export] public AudioStreamPlayer MusicPlayer;

        [Export] public AnimationPlayer PlayerAnimator;

        private Parameters _parameters = null;
        public class Parameters
        {
            public Environment SkyBoxEnvironment;

            public string PreviousLevelScenePath;
            public string HomeWorldScenePath;

            public double PlayerAnimationTime;
            public Transform3D PlayerStartPos;
            public Transform3D CameraStartPos;
        }

        private Transform3D _cameraRestPos;
        private PackedScene _loadedHomeWorld = null;

        private readonly StateMachine _stateMachine = new StateMachine();

        public MissionStatsScreen()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            _cameraRestPos = Camera.GlobalTransform;

            Callable.From(() =>
            {
                // If Initialize() hasn't already been called by the time this
                // runs, then this scene must have been launched directly from
                // the editor.
                //
                // Launching this scene directly from the editor is supported
                // to make testing easier.  It _does_ mean we need to call
                // Initialize() with dummy values, though.
                bool launchedDirectlyFromEditor = _parameters == null;
                if (launchedDirectlyFromEditor)
                {
                    Initialize(GetTestParameters());
                }
            }).CallDeferred();
        }

        private Parameters GetTestParameters() => new()
        {
            PlayerStartPos = Transform3D.Identity,
            CameraStartPos = Transform3D.Identity
                .Translated(new Vector3(0, -3, 0))
                .RotatedLocal(Vector3.Right, Mathf.DegToRad(90)),

            PlayerAnimationTime = 1.2,

            // Don't overwrite the default values that were assigned
            // in the inspector.
            PreviousLevelScenePath = PreviousLevelScenePath,
            HomeWorldScenePath = HomeWorldScenePath,
        };

        public void Initialize(Parameters parameters)
        {
            _parameters = parameters;

            Camera.Environment = _parameters.SkyBoxEnvironment;
            // TODO: Adjust the sun

            PreviousLevelScenePath = parameters.PreviousLevelScenePath;
            HomeWorldScenePath = parameters.HomeWorldScenePath;

            StartLoadingHomeWorldInBackground();
            _stateMachine.ChangeState<Entering>();
        }

        private void StartLoadingHomeWorldInBackground()
        {
            // HACK: Using a C# thread instead of ResourceLoader.LoadThreadedRequest()
            // to avoid a non-deterministic deadlock.
            // See https://github.com/godotengine/godot/issues/107548
            new System.Threading.Thread(() =>
            {
                var loadedScene = ResourceLoader.Load<PackedScene>(HomeWorldScenePath);
                SetDeferred(nameof(_loadedHomeWorld), loadedScene);
            }).Start();
        }

        private bool DoneLoading() => _loadedHomeWorld != null;

        private partial class Entering : State<MissionStatsScreen>
        {
            private const double Duration = 1.25;
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = 0;
                Self.Backdrop.Transparency = 1;

                Self.Camera.GlobalTransform = Self._parameters.CameraStartPos;
                Self.PlayerModel.GlobalTransform = Self._parameters.PlayerStartPos;

                Self.PlayerAnimator.PlaySection("Glide", Self._parameters.PlayerAnimationTime);
                Self.PlayerAnimator.Advance(0);
                Self.PlayerAnimator.Play("Idle", customBlend: Duration);
            }

            public override void _Process(double delta)
            {
                _timer += delta;
                float t = (float)(_timer / Duration);
                t = Mathf.Min(t, 1);
                t = MathUtils.LerpSinusoidal(0, 1, t);

                var parms = Self._parameters;

                Self.Backdrop.Transparency = 1f - t;

                Self.PlayerModel.GlobalTransform = parms
                    .PlayerStartPos
                    .InterpolateWith(Transform3D.Identity, t);

                Self.Camera.GlobalTransform = parms
                    .CameraStartPos
                    .InterpolateWith(Self._cameraRestPos, t);

                if (_timer >= Duration)
                    ChangeState<ShowingStats>();
            }
        }

        private partial class ShowingStats : State<MissionStatsScreen>
        {
            public override void OnStateEntered()
            {
                Self.MusicPlayer.Play();
            }

            public override void _Input(InputEvent ev)
            {
                if (InputService.SkipJustPressed(ev))
                    ChangeState<WaitingForLoad>();
            }
        }

        private partial class WaitingForLoad : State<MissionStatsScreen>
        {
            public override void _Process(double delta)
            {
                if (Self.DoneLoading())
                    ChangeState<Exiting>();
            }
        }

        private partial class Exiting : State<MissionStatsScreen>
        {
            private const double TweenDuration = 1;
            private const double ExtraWaitDuration = 0.5;
            private const float CameraDist = 6;

            private Node3D _loadedHomeWorldRoot;
            private double _timer;

            private Transform3D _playerStart;
            private Transform3D _playerEnd;

            private Transform3D _cameraStart;
            private Transform3D _cameraEnd;

            private float _volumeStart;


            public override void OnStateEntered()
            {
                GD.Print("Exiting");
                _loadedHomeWorldRoot = Self._loadedHomeWorld.Instantiate<Node3D>();
                _timer = 0;

                _volumeStart = Self.MusicPlayer.VolumeLinear;

                var portal = GetTargetPortal();

                Transform3D playerSpawn = portal.PlayerSpawn.GetGlobalTransformOutsideOfTree();
                _playerStart = Self.PlayerModel.GlobalTransform;
                _playerEnd = playerSpawn
                    .Translated(Vector3.Up * portal.ExitAnimationStartHeight)
                    .TranslatedLocal(Vector3.Back * (CameraDist + 2));

                _cameraStart = Self.Camera.GlobalTransform;
                _cameraEnd = _playerEnd
                    .TranslatedLocal(Vector3.Forward * CameraDist)
                    .LookingAt(_playerEnd.Origin)
                    .Translated(Vector3.Up * 2);

                Self.PlayerAnimator.Play("Levitate", 0.5);

                // HACK: Sneakily switch to using the portal's skybox while
                // the backdrop is still covering it.
                //
                // The portal's skybox may not match the level we're exiting if,
                // for example, the correct portal couldn't be found and we need
                // to use a fallback.
                //
                // The synchronous load won't cause any hiccups if the skyboxes
                // match(since it'll already be cached)
                Self.Camera.Environment = ResourceLoader.Load<Environment>(portal.SkyboxEnvironment);
            }

            public override void _Process(double delta)
            {
                _timer += delta;
                float t = (float)(_timer / TweenDuration);
                t = Mathf.Min(t, 1);
                t = MathUtils.LerpSinusoidal(0, 1, t);

                Self.PlayerModel.GlobalTransform = _playerStart.InterpolateWith(_playerEnd, t);
                Self.Camera.GlobalTransform = _cameraStart.InterpolateWith(_cameraEnd, t);
                Self.Backdrop.Transparency = t;
                Self.MusicPlayer.VolumeLinear = Mathf.Lerp(_volumeStart, 0, t);

                // TODO: Tween the sun, too.

                if (_timer >= TweenDuration + ExtraWaitDuration)
                    GoToHomeWorld();
            }

            private void GoToHomeWorld()
            {
                LevelTransitionManager.Instance.ChangeSceneToNode(_loadedHomeWorldRoot);

                var realPlayer = _loadedHomeWorldRoot.FindNode<Player>();
                realPlayer.Animator.Play(Self.PlayerAnimator.AssignedAnimation, 0);
                realPlayer.Animator.Seek(Self.PlayerAnimator.CurrentAnimationPosition, true);
                realPlayer.Animator.Advance(0);

                var portal = GetTargetPortal();
                portal.PlayExitAnimation();
            }

            private Portal GetTargetPortal()
            {
                var matchingPortal = _loadedHomeWorldRoot
                    .EnumerateDescendantsOfType<Portal>()
                    .FirstOrDefault(p => p.TargetLevel == Self.PreviousLevelScenePath);

                if (matchingPortal != null)
                    return matchingPortal;

                // No matching portal was found, so fall back to the first portal.
                return _loadedHomeWorldRoot
                    .EnumerateDescendantsOfType<Portal>()
                    .First();

                // TODO: What should the fallback be if there are _no_ portals?
                // Should there even _be_ a fallback for that case?
            }
        }
    }
}