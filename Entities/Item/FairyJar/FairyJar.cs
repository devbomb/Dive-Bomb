using System.Diagnostics;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class FairyJar : StaticBody3D, IBreakable
    {
        [Signal] public delegate void BreakRejectedEventHandler();

        [Export] public int GemCost { get; set; }

        /// <summary>
        /// Optional.  If specified(IE: not null or the empty string), other
        /// entities can use it to get a reference to this fairy, IE to check
        /// if it's been freed.
        ///
        /// Not to be confused with <see cref="SaveKey"/>!
        /// </summary>
        [Export] public string targetname;

        public bool VulnerableToKick => CanBreak();
        public bool VulnerableToRoll => CanBreak();
        public bool CausesBonk => !CanBreak();

        [ExportGroup("Internal")]
        [Export] public AnimationPlayer Animator;

        [Export] public AudioStreamPlayer ShatterSound;
        [Export] public AudioStreamPlayer JingleSound;
        [Export] public AudioStreamPlayer MusicDuckTrigger;

        [Export] public Node3D Model;
        [Export] public Node3D Glass;

        [Export] public GpuParticles3D GlassParticles;
        [Export] public CollisionShape3D CollisionShape;

        public bool CanBreak() => EnoughGems() || this.IsTimeTrialMode();
        public bool EnoughGems() => (this.GetLevel()?.TotalGems ?? 0) >= GemCost;
        public bool ShowPriceTag() => GemCost > 0 && !this.IsTimeTrialMode();

        public bool IsReadyForGuide() => _stateMachine.CurrentState is Rescued;

        /// <summary>
        /// The Id used to identify this fairy in the save file.
        /// Will be equal to <see cref="targetname"/> if that value is provided.
        /// Otherwise, it will default to a value derived from this node's path.
        /// </summary>
        public string SaveKey { get; private set; }

        private readonly StateMachine _stateMachine = new StateMachine();
        private Transform3D _initialModelPos;
        private float _initialCameraYawRad;
        private Node3D _camTarget;

        private Player Player;

        public override void _Ready()
        {
            SaveKey = GenerateSaveKey();
            _initialModelPos = Model.GlobalTransform;

            SignalBus.Instance.LevelReset += Reset;
            AddChild(_stateMachine);

            Reset();
        }

        public void Reset()
        {
            Model.GlobalTransform = _initialModelPos;
            Model.ResetPhysicsInterpolation3D();

            Animator.Play("RESET");
            Animator.Advance(0);

            if (IsFree())
                _stateMachine.ChangeState<Rescued>();
            else
                _stateMachine.ChangeState<Idle>();
        }

        public bool IsFree()
        {
            return this.GetLevel()
                ?.GetProgress()
                ?.CollectedFairies
                ?.Contains(SaveKey) ?? false;
        }

        public void OnBroken()
        {
            // TODO: I suspect this might be getting called twice sometimes, but
            // very rarely.  Log if that happens.
            if (_stateMachine.CurrentState is not Idle)
            {
                string msg = "OnBroken() was called outside of the idle state.  Is it firing twice?!";
                GD.PushError(msg);
                throw new System.Exception(msg);
            }

            var level = this.GetLevel();
            if (level != null)
            {
                level.GetProgress().CollectedFairies.Add(SaveKey);
                level.GetProgress().SpentGems += GemCost;
            }

            SignalBus.Instance.EmitItemCollected();

            if (this.IsTimeTrialMode())
            {
                _stateMachine.ChangeState<QuickRescue>();
                return;
            }

            SaveFileManager.Current.CurrentLevelVisit.GemsSpent += GemCost;
            SaveFileManager.Current.CurrentLevelVisit.FairiesFound++;
            SaveFileManager.Instance.RequestAutosave();

            _stateMachine.ChangeState<Shattering>();
        }

        public void OnBreakRejected()
        {
            EmitSignal(SignalName.BreakRejected);
        }

        private void SetPausedForCutscene(bool paused)
        {
            GetTree().Paused = paused;

            ProcessMode = paused
                ? ProcessModeEnum.Always
                : ProcessModeEnum.Inherit;

            Player.ProcessMode = ProcessMode;
            GetTree().FindNode<BackgroundMusicPlayer>().ProcessMode = ProcessMode;
            MusicDuckTrigger.Playing = paused;
        }

        private string GenerateSaveKey()
        {
            if (!string.IsNullOrEmpty(targetname))
                return targetname;

            var builder = new System.Text.StringBuilder();
            Visit(this);
            return builder.ToString();

            void Visit(Node n)
            {
                if (n.GetParent() == GetTree().Root)
                {
                    builder.Append(n.Name);
                    return;
                }

                Visit(n.GetParent());
                builder.Append("/");
                builder.Append(n.GetIndex());
            }
        }

        private bool HasGuide()
        {
            return GetTree().Root
                .EnumerateDescendantsOfType<FairyGuide>()
                .Any(f => f.FairyId == targetname);
        }

        private class Idle : State<FairyJar>
        {
            public override void OnStateEntered()
            {
                Self.Player = GetTree().FindNode<Player>();
                Self.Visible = true;
                Self.Glass.Visible = true;
                Self.Animator.Play("PoundingOnGlass");

                Self.CollisionShape.Disabled = false;
            }

            public override void OnStateExited()
            {
                Self.CollisionShape.Disabled = true;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                Self.Model.GlobalRotation = Self.GlobalPosition
                    .DirectionTo(Self.Player.GlobalPosition)
                    .Flattened()
                    .Normalized()
                    .ForwardToEulerAnglesRad();
            }
        }

        private class Shattering : State<FairyJar>
        {
            private const float Duration = 2f;
            private const float CameraMoveDelay = 1f;
            private const float CameraMoveDuration = Duration - CameraMoveDelay;
            private const float TimeScale = 0.5f;

            private static float PlayerJumpSpeed => Player.Jump.InitVSpeed;
            private static float PlayerGravity => Player.Jump.ShortHopGravity;

            private Player _player => Self.Player;

            private bool _playerLanded;
            private bool _cameraManhandled;
            private float _timer;

            public override void OnStateEntered()
            {
                _cameraManhandled = false;
                _timer = 0;

                // Pause the game (except the player and fairy) during the
                // cutscene to prevent the player from getting hit by enemies.
                // Don't worry, the pause menu won't open if the game is already
                // paused by something else.
                Self.SetPausedForCutscene(true);

                // Hijack control of the player.  We're going to manipulate them
                // for the cutscene.
                _player.ChangeState<PlayerManhandledState>();
                _player.Velocity = Vector3.Up * PlayerJumpSpeed;
                _player.Velocity += _player.GlobalForward() * -1;
                _playerLanded = false;

                // Krrsssh!!!  Shatter the glass!
                Self.Glass.Visible = false;
                Self.GlassParticles.Emitting = true;
                Self.ShatterSound.Play();

                Self.Animator.Play("Shatter");

                // Do a slow-mo effect until the player lands
                Engine.TimeScale = TimeScale;
            }

            public override void OnStateExited()
            {
                Self.SetPausedForCutscene(false);
                Engine.TimeScale = 1;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _timer += delta / (float)Engine.TimeScale;

                ApplyGravityToPlayer(delta);
                MoveCamera();

                if (_playerLanded && !_player.Animator.IsPlaying() && _timer >= Duration)
                    ChangeState<FlyingToPlayer>();
            }

            private void ApplyGravityToPlayer(float delta)
            {
                _player.Velocity += Vector3.Down * PlayerGravity * delta;
                _player.MoveAndSlide();

                if (_player.IsOnFloor() && !_playerLanded)
                {
                    _playerLanded = true;
                    _player.Velocity = Vector3.Zero;
                    Engine.TimeScale = 1;
                    _player.Animator.PlaySection("ParachuteLand", endTime: 0.75f);
                }
            }

            private void MoveCamera()
            {
                if (_timer < CameraMoveDelay)
                    return;

                if (!_cameraManhandled)
                {
                    Self._initialCameraYawRad = _player.Camera.GlobalRotation.Y;
                    Self._camTarget = KissPointClosestToCurrentCamPos();

                    _player.Camera.StartManhandling(CamTargetPos(), CameraMoveDuration);
                    _cameraManhandled = true;
                }

                _player.Camera.ManhandledPosition = CamTargetPos();
            }

            private Node3D KissPointClosestToCurrentCamPos()
            {
                var player = Self.Player;
                var cam = player.Camera;

                var rightPoint = player.FairyKissCamRightPoint;
                var leftPoint = player.FairyKissCamLeftPoint;

                Vector3 playerPosCameraSpace = cam.ToLocal(player.GlobalPosition);
                Vector3 jarPosCameraSpace = cam.ToLocal(Self.GlobalPosition);

                return playerPosCameraSpace.X > jarPosCameraSpace.X
                    ? leftPoint
                    : rightPoint;
            }

            private Transform3D CamTargetPos()
            {
                var virtuallyRotatedPlayerPos = Transform3D.Identity
                    .Rotated(Vector3.Up, Self._initialCameraYawRad - Self._camTarget.Rotation.Y)
                    .Translated(_player.GlobalPosition);

                return virtuallyRotatedPlayerPos * Self._camTarget.Transform;
            }
        }

        private class FlyingToPlayer : State<FairyJar>
        {
            private const float Duration = 0.5f;

            private Transform3D _start;

            private float _playerInitialYawRad;
            private float _playerTargetYawRad;

            private float _timer;

            public override void OnStateEntered()
            {
                Self.SetPausedForCutscene(true);
                Self.Animator.Play("Hovering", 0.1f);
                Self.Player.Animator.Play("Idle");
                Self.JingleSound.Play();

                _start = Self.Model.GlobalTransform;
                _timer = 0;

                // Start rotating the player such that the camera will be
                // pointing where it originally was when the cutscene is over.
                _playerInitialYawRad = Self.Player.YawRad;
                _playerTargetYawRad = Self._initialCameraYawRad - Self._camTarget.Rotation.Y;
            }

            public override void OnStateExited()
            {
                Self.SetPausedForCutscene(false);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                RotatePlayer();
                MoveFairy();

                if (_timer > Duration)
                {
                    ChangeState<KissingPlayer>();
                }
            }

            private void MoveFairy()
            {
                float t = _timer / Duration;
                t = Mathf.SmoothStep(0, 1, t);

                var player = Self.Player;
                var target = player.FairyKissPoint.GlobalTransform;
                Self.Model.GlobalTransform = _start.InterpolateWith(target, t);
            }

            private void RotatePlayer()
            {
                float t = _timer / Duration;
                t = Mathf.Clamp(t, 0, 1);
                Self.Player.YawRad = Mathf.LerpAngle(_playerInitialYawRad, _playerTargetYawRad, t);
            }
        }

        private class KissingPlayer : State<FairyJar>
        {
            public override void OnStateEntered()
            {
                Self.SetPausedForCutscene(true);
                Self.Animator.Play("Kiss", 0.3f);
            }

            public override void OnStateExited()
            {
                Self.SetPausedForCutscene(false);
                Self.Player.ChangeState<PlayerWalkState>();
                Self.Player.Camera.StartFollowing(0.75f);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                if (!Self.Animator.IsPlaying())
                {
                    if (!Self.HasGuide())
                        ChangeState<FlyingAway>();
                    else
                        ChangeState<Rescued>();
                }
            }
        }

        private class FlyingAway : State<FairyJar>
        {
            private const float Duration = 0.75f;
            private float _timer;

            public override void OnStateEntered()
            {
                _timer = 0;
                Self.Animator.Play("FlyAwayHigh");
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                if (_timer > Duration)
                    ChangeState<Rescued>();
            }
        }

        private class QuickRescue : State<FairyJar>
        {
            private const float MoveToCameraDuration = 0.5f;

            private Transform3D _initialOffsetFromCamera;
            private Transform3D _targetOffsetFromCamera;

            private float _timer;

            public override void OnStateEntered()
            {
                Self.Glass.Visible = false;
                Self.GlassParticles.Emitting = true;
                Self.ShatterSound.Play();

                Self.Animator.Play("Shatter");

                if (!Self.HasGuide())
                    Self.Animator.Queue("FlyAway");

                _initialOffsetFromCamera = SaveOffsetFromCamera();
                _targetOffsetFromCamera = Self.Player.Camera.TimeTrialFairyRescuePos.Transform;

                _timer = 0;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;
                float t = Mathf.Min(_timer / MoveToCameraDuration, 1);
                t = MathUtils.LerpSinusoidal(0, 1, t);

                var offsetFromCamera = _initialOffsetFromCamera.InterpolateWith(_targetOffsetFromCamera, t);
                var camera = GetTree().Root.GetCamera3D();
                Self.Model.GlobalTransform = camera.GlobalTransform * offsetFromCamera;

                if (!Self.Animator.IsPlaying())
                    ChangeState<Rescued>();
            }

            private Transform3D SaveOffsetFromCamera()
            {
                var camera = GetTree().Root.GetCamera3D();
                return camera.GlobalTransform.AffineInverse() * Self.Model.GlobalTransform;
            }
        }

        private class Rescued : State<FairyJar>
        {
            public override void OnStateEntered()
            {
                Self.Visible = false;
            }
        }
    }
}