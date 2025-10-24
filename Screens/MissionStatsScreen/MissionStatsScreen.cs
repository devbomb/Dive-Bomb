using System.Collections.Generic;
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
        [Export] public Node3D ChalkboardModel;
        [Export] public Camera3D Camera;


        [Export] public AnimationPlayer PlayerAnimator;
        [Export] public AnimationPlayer ChalkboardAnimator;
        [Export] public AnimationPlayer ContinueFlashAnimator;

        [Export] public Control ContinueButtonPrompt;

        [ExportGroup("Fairies")]
        [Export] public PackedScene FairyPrefab;
        [Export] public Path3D FairyPath;

        [ExportGroup("Gems")]
        [Export] public GpuParticles3D GemSpawner;
        [Export] public Godot.Collections.Dictionary<GemColor, Color> GemColors;

        [ExportGroup("Sounds")]
        [Export] public AudioStreamPlayer MusicPlayer;
        [Export] public AudioStreamPlayer GemCountSound;
        [Export] public AudioStreamPlayer GemSpentSound;
        [Export] public AudioStreamPlayer DeathCountIncreaseSound;
        [Export] public AudioStreamPlayer ContinuePressedSound;

        [ExportGroup("Labels")]
        [ExportSubgroup("This level")]
        [Export] public Label TotalFairiesLabel;
        [Export] public Control FairiesLabelHolder;
        [Export] public Label FairiesLabelNumber;

        [Export] public Label TotalGemsLabel;
        [Export] public Label GemsFoundLabel;
        [Export] public Label GemsSpentLabel;

        [Export] public Control DeathsLabelHolder;
        [Export] public Label DeathsLabelNumber;

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
                    SaveFileManager.Current.CurrentLevelVisit = GetTestStats();
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

        private SaveFile.LevelVisit GetTestStats() => new()
        {
            Deaths = 10,
            FairiesFound = 3,
            GemsSpent = 200,
            GemsFound = new()
            {
                { GemColor.Red, 30 },
                { GemColor.Green, 31 },
                { GemColor.Purple, 34 },
                { GemColor.Yellow, 11 },
                { GemColor.Magenta, 1 }
            }
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
                Self.PlayerAnimator.Play("BoomDiggityScat", customBlend: Duration);

                Self.ChalkboardModel.Visible = false;
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
            private Coroutine _coroutine;

            public override void OnStateEntered()
            {
                Self.MusicPlayer.Play();
                _coroutine = new Coroutine(ShowStatsCoroutine());

                Self.TotalFairiesLabel.Text = TotalFairiesBeforeCounting().ToString();
                Self.TotalGemsLabel.Text = TotalGemsBeforeCounting().ToString();

                Self.ChalkboardModel.Visible = false;
                Self.FairiesLabelHolder.Visible = false;
                Self.GemsFoundLabel.Visible = false;
                Self.GemsSpentLabel.Visible = false;
                Self.DeathsLabelHolder.Visible = false;
            }

            public override void OnStateExited()
            {
                var saveFile = SaveFileManager.Current;
                var stats = saveFile.CurrentLevelVisit;

                Self.TotalFairiesLabel.Text = saveFile.TotalFairyCount.ToString();
                Self.TotalGemsLabel.Text = saveFile.TotalGemCount.ToString();

                Self.ChalkboardModel.Visible = true;
                Self.ChalkboardAnimator.Play("RESET");
                Self.ChalkboardAnimator.Advance(0);

                Self.FairiesLabelHolder.Visible = true;
                Self.FairiesLabelNumber.Text = stats.FairiesFound.ToString();

                Self.GemsFoundLabel.Visible = true;
                Self.GemsFoundLabel.Text = $"Gems found: {stats.TotalGemsFound}";

                Self.GemsSpentLabel.Visible = true;
                Self.GemsSpentLabel.Text = $"Gems spent: {stats.GemsSpent}";

                Self.DeathsLabelHolder.Visible = true;
                Self.DeathsLabelNumber.Text = stats.Deaths.ToString();

                Self.FairyPath.Visible = false;
            }

            public override void _Input(InputEvent ev)
            {
                if (InputService.SkipJustPressed(ev))
                    ChangeState<LettingPlayerReadStats>();
            }

            public override void _Process(double delta)
            {
                _coroutine.Tick(delta);

                if (_coroutine.Done)
                    ChangeState<LettingPlayerReadStats>();
            }

            private IEnumerator<YieldInstruction> ShowStatsCoroutine()
            {
                Self.ChalkboardModel.Visible = true;
                Self.ChalkboardAnimator.Play("SwingIn");
                Self.ChalkboardAnimator.Advance(0);
                while (Self.ChalkboardAnimator.IsPlaying())
                    yield return default;

                yield return Coroutine.WaitFor(SlideLabelIn(Self.FairiesLabelHolder, 0.1));
                yield return Coroutine.WaitSeconds(0.25);
                yield return Coroutine.WaitFor(CountFairies());
                yield return Coroutine.WaitSeconds(0.5);

                yield return Coroutine.WaitFor(SlideLabelIn(Self.GemsFoundLabel, 0.1));
                yield return Coroutine.WaitSeconds(0.25);
                yield return Coroutine.WaitFor(CountGemsFound());
                yield return Coroutine.WaitSeconds(0.5);

                yield return Coroutine.WaitFor(SlideLabelIn(Self.GemsSpentLabel, 0.1));
                yield return Coroutine.WaitSeconds(0.25);
                yield return Coroutine.WaitFor(CountGemsSpent());
                yield return Coroutine.WaitSeconds(0.5);

                yield return Coroutine.WaitFor(SlideLabelIn(Self.DeathsLabelHolder, 0.1));
                yield return Coroutine.WaitSeconds(0.25);
                yield return Coroutine.WaitFor(CountDeaths());
                yield return Coroutine.WaitSeconds(0.5);
            }

            private IEnumerator<YieldInstruction> CountFairies()
            {
                const double countInterval = 1.0 / 3;
                const double pulseDuration = countInterval / 2;

                var stats = SaveFileManager.Current.CurrentLevelVisit;
                int totalFairies = TotalFairiesBeforeCounting();
                int fairies = 0;

                Self.FairiesLabelNumber.Text = fairies.ToString();

                for (int i = 0; i < stats.FairiesFound; i++)
                {
                    Self.DeathCountIncreaseSound.Play(); // TODO: Play a different sound
                    SpawnFairy();

                    fairies++;
                    Self.FairiesLabelNumber.Text = fairies.ToString();

                    totalFairies++;
                    Self.TotalFairiesLabel.Text = totalFairies.ToString();

                    yield return Coroutine.WaitFor(PulseLabel(pulseDuration, Self.FairiesLabelNumber));
                    yield return Coroutine.WaitSeconds(countInterval - pulseDuration);
                }

                void SpawnFairy()
                {
                    var fairy = Self.FairyPrefab.Instantiate<PathFollow3D>();
                    Self.FairyPath.AddChild(fairy);
                }
            }

            private IEnumerator<YieldInstruction> CountGemsFound()
            {
                var rng = new System.Random();

                const double countInterval = 2.0 / 60;
                const double maxDuration = 1.25;

                const double sfxCooldown = countInterval * 2;
                double sfxCooldownTimer = 0;

                var stats = SaveFileManager.Current.CurrentLevelVisit;
                var untalliedGems = stats.GemsFound.ToDictionary();

                int globalTotalGems = TotalGemsBeforeCounting();
                int levelTotalGems = 0;

                const double maxCycles = maxDuration / countInterval;
                double totalIndividualGems = untalliedGems.Sum(kvp => kvp.Value);
                int gemsPerCycle = Mathf.CeilToInt(totalIndividualGems / maxCycles);

                while (!AllGemsCounted())
                {
                    if (sfxCooldownTimer <= 0)
                    {
                        Self.GemCountSound.Play();
                        sfxCooldownTimer += sfxCooldown;
                    }

                    for (int i = 0; i < gemsPerCycle && !AllGemsCounted(); i++)
                    {
                        GemColor color = rng.PickFromWeighted(untalliedGems);
                        SpawnAndCountGem(color);
                    }

                    yield return Coroutine.WaitSeconds(countInterval);
                    sfxCooldownTimer -= countInterval;
                }

                globalTotalGems = TotalGemsBeforeCounting() + stats.TotalGemsFound;
                Self.TotalGemsLabel.Text = globalTotalGems.ToString();
                Self.GemsFoundLabel.Text = $"Gems found: {stats.TotalGemsFound}";

                bool AllGemsCounted() => untalliedGems.Sum(kvp => kvp.Value) <= 0;

                void SpawnAndCountGem(GemColor color)
                {
                    untalliedGems[color]--;

                    globalTotalGems += (int)color;
                    levelTotalGems += (int)color;

                    Self.GemSpawner.EmitParticle(
                        default,
                        default,
                        Self.GemColors[color],
                        default,
                        (uint)GpuParticles3D.EmitFlags.Color
                    );

                    Self.TotalGemsLabel.Text = globalTotalGems.ToString();
                    Self.GemsFoundLabel.Text = $"Gems found: {levelTotalGems}";
                }
            }

            private IEnumerator<YieldInstruction> CountGemsSpent()
            {
                const double countInterval = 2.0 / 60;
                const double sfxCooldown = countInterval * 2;
                const double maxDuration = 1.0;
                double skipTimer = 0;
                double sfxCooldownTimer = 0;

                var stats = SaveFileManager.Current.CurrentLevelVisit;
                int globalTotalGems = TotalGemsBeforeCounting() + stats.TotalGemsFound;

                for (int i = 0; i < stats.GemsSpent; i++)
                {
                    if (sfxCooldownTimer <= 0)
                    {
                        Self.GemSpentSound.Play();
                        sfxCooldownTimer += sfxCooldown;
                    }

                    globalTotalGems--;
                    Self.TotalGemsLabel.Text = globalTotalGems.ToString();
                    Self.GemsSpentLabel.Text = $"Gems spent: {i + 1}";
                    yield return Coroutine.WaitSeconds(countInterval);

                    // Cut to the chase if it's taking too long
                    skipTimer += countInterval;
                    sfxCooldownTimer -= countInterval;

                    if (skipTimer >= maxDuration)
                        break;
                }

                globalTotalGems = SaveFileManager.Current.TotalGemCount;
                Self.TotalGemsLabel.Text = globalTotalGems.ToString();
                Self.GemsSpentLabel.Text = $"Gems spent: {stats.GemsSpent}";
            }

            private IEnumerator<YieldInstruction> CountDeaths()
            {
                const double countInterval = 1.0 / 3;
                const double pulseDuration = countInterval / 2;

                var stats = SaveFileManager.Current.CurrentLevelVisit;
                int deaths = 0;
                Self.DeathsLabelNumber.Text = deaths.ToString();

                for (int i = 0; i < stats.Deaths; i++)
                {
                    Self.DeathCountIncreaseSound.Play();
                    deaths++;
                    Self.DeathsLabelNumber.Text = deaths.ToString();

                    yield return Coroutine.WaitFor(PulseLabel(pulseDuration, Self.DeathsLabelNumber));
                    yield return Coroutine.WaitSeconds(countInterval - pulseDuration);
                }
            }

            private IEnumerator<YieldInstruction> SlideLabelIn(Control label, double duration)
            {
                label.Visible = true;

                var start = new Vector2(-100, 0);
                var end = new Vector2(0, 0);

                label.Position = start;

                double timer = 0;
                while (timer < duration)
                {
                    timer += Self.GetProcessDeltaTime();
                    label.Position = start.Lerp(end, (float)(timer / duration));
                    yield return default;
                }

                label.Position = end;
            }

            private IEnumerator<YieldInstruction> PulseLabel(
                double pulseDuration,
                Label label
            )
            {
                // Ensure the pivot for the pulsing animation is always the
                // center of the label, regardless of how many digits there
                // are.
                label.UpdateMinimumSize();
                label.PivotOffset = label.GetMinimumSize() / 2;

                double timer = 0;
                while (true)
                {
                    timer += Self.GetProcessDeltaTime();
                    float t = (float)(timer / pulseDuration);
                    float pingPongT = Mathf.PingPong(t * 2, 1);

                    if (timer < pulseDuration)
                    {
                        Vector2 maxScale = Vector2.One * 1.5f;
                        label.Scale = Vector2.One.Lerp(maxScale, pingPongT);
                        yield return default;
                    }
                    else
                    {
                        yield break;
                    }
                }
            }

            private int TotalFairiesBeforeCounting()
            {
                var stats = SaveFileManager.Current.CurrentLevelVisit;
                int totalFairies = SaveFileManager.Current.TotalFairyCount;
                totalFairies -= stats.FairiesFound;

                return totalFairies;
            }

            private int TotalGemsBeforeCounting()
            {
                var stats = SaveFileManager.Current.CurrentLevelVisit;
                int totalGemsBeforeCounting = SaveFileManager.Current.TotalGemCount;
                totalGemsBeforeCounting += stats.GemsSpent;
                totalGemsBeforeCounting -= stats.TotalGemsFound;

                return totalGemsBeforeCounting;
            }
        }

        private partial class LettingPlayerReadStats : State<MissionStatsScreen>
        {
            public override void _Input(InputEvent ev)
            {
                if (InputService.SkipJustPressed(ev))
                {
                    ChangeState<WaitingForLoad>();
                    Self.ContinuePressedSound.Play();
                    Self.ContinueFlashAnimator.Play("Flash");
                    Self.MusicPlayer.Stop();
                }
            }

            public override void OnStateEntered()
            {
                Self.ContinueButtonPrompt.Visible = true;
            }

            public override void OnStateExited()
            {
                Self.ContinueButtonPrompt.Visible = false;
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

            public override void OnStateEntered()
            {
                GD.Print("Exiting");
                _loadedHomeWorldRoot = Self._loadedHomeWorld.Instantiate<Node3D>();
                _timer = 0;

                var portal = GetTargetPortal();
                Transform3D playerSpawn = portal.PlayerSpawn.GetGlobalTransformOutsideOfTree();
                playerSpawn.Origin = Self.PlayerModel.GlobalTransform.Origin;

                _playerStart = Self.PlayerModel.GlobalTransform;
                _playerEnd = playerSpawn;

                _cameraStart = Self.Camera.GlobalTransform;
                _cameraEnd = _playerEnd
                    .TranslatedLocal(Vector3.Forward * CameraDist)
                    .LookingAt(_playerEnd.Origin)
                    .Translated(Vector3.Up * 2);

                Self.PlayerAnimator.Play("Levitate", 0.5);
                Self.ChalkboardAnimator.Play("Vanish");

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