using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class Fairy : StaticBody3D, IBreakable
    {
        [Signal] public delegate void BreakRejectedEventHandler();

        [Export] public int GemCost { get; set; }

        public bool VulnerableToKick => CanBreak();
        public bool VulnerableToRoll => CanBreak();
        public bool CausesBonk => !CanBreak();

        public bool CanBreak() => EnoughGems() || IsTimeTrialMode();
        public bool EnoughGems() => SaveFile.Current.TotalGemCount >= GemCost;
        public bool ShowPriceTag() => GemCost > 0 && !IsTimeTrialMode();

        private readonly StateMachine _stateMachine = new StateMachine();
        private Transform3D _initialModelPos;

        private AnimationPlayer Animator => GetNode<AnimationPlayer>("%AnimationPlayer");
        private AudioStreamPlayer ShatterSound => GetNode<AudioStreamPlayer>("%ShatterSound");
        private AudioStreamPlayer JingleSound => GetNode<AudioStreamPlayer>("%JingleSound");

        private Node3D Model => GetNode<Node3D>("%Model");
        private Node3D Glass => GetNode<Node3D>("%Glass");
        private GpuParticles3D GlassParticles => GetNode<GpuParticles3D>("%GlassParticles");
        private CollisionShape3D CollisionShape => GetNode<CollisionShape3D>("%CollisionShape");

        private Camera3D CutsceneCam => GetNode<Camera3D>("%CutsceneCam");

        private Player Player;

        public override void _Ready()
        {
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

            if (SaveFile.Current.CurrentMapProgress.CollectedFairies.Contains(GetSaveKey()))
                _stateMachine.ChangeState<Rescued>();
            else
                _stateMachine.ChangeState<Idle>();
        }

        public string GetSaveKey()
        {
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

        public void OnBroken()
        {
            if (IsTimeTrialMode())
            {
                _stateMachine.ChangeState<QuickRescue>();
            }
            else
            {
                SaveFile.Current.SpendGems(GemCost);
                _stateMachine.ChangeState<Shattering>();
            }
        }

        public void OnBreakRejected()
        {
            EmitSignal(SignalName.BreakRejected);
        }

        private bool IsTimeTrialMode()
        {
            return GetTree().Root.FindNode<TimeTrialManager>()?.IsTimeTrialMode ?? false;
        }

        private void SetPausedForCutscene(bool paused)
        {
            GetTree().Paused = paused;

            ProcessMode = paused
                ? ProcessModeEnum.Always
                : ProcessModeEnum.Inherit;

            Player.ProcessMode = ProcessMode;
        }

        private class Idle : State<Fairy>
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

        private class Shattering : State<Fairy>
        {
            private const float Duration = 2f;
            private const float CameraMoveDelay = 1f;
            private const float TimeScale = 0.5f;

            private static float PlayerJumpSpeed => Player.Jump.InitVSpeed;
            private static float PlayerGravity => Player.Jump.ShortHopGravity;

            private Player _player => Self.Player;
            private Node3D _camTarget;

            private bool _playerLanded;
            private float _timer;

            public override void OnStateEntered()
            {
                SaveFile.Current.CollectFairy(SaveFile.Current.CurrentMap, Self.GetSaveKey());

                // Pause the game (except the player and fairy) during the
                // cutscene to prevent the player from getting hit by enemies.
                // Don't worry, the pause menu won't open if the game is already
                // paused by something else.
                Self.SetPausedForCutscene(true);

                // Hijack control of the player.  We're going to manipulate them
                // for the cutscene.
                Self.Player.ChangeState<PlayerManhandledState>();
                _player.Velocity = Vector3.Up * PlayerJumpSpeed;
                _player.Velocity += _player.GlobalForward() * -1;
                _playerLanded = false;

                // Krrsssh!!!  Shatter the glass!
                Self.Glass.Visible = false;
                Self.GlassParticles.Emitting = true;
                Self.ShatterSound.Play();

                Self.Animator.Play("Shatter");

                // Start moving the camera into position
                _camTarget = CamTarget();
                Self.CutsceneCam.GlobalTransform = _player.Camera.GlobalTransform;
                Self.CutsceneCam.MakeCurrent();
                Self.CutsceneCam.ResetPhysicsInterpolation3D();

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

                MoveCamera();
                ApplyGravityToPlayer(delta);

                if (_playerLanded && !_player.Animator.IsPlaying() && _timer >= Duration)
                    ChangeState<FlyingToPlayer>();
            }

            private void MoveCamera()
            {
                float croppedDuration = Duration - CameraMoveDelay;
                float croppedTimer = _timer - CameraMoveDelay;
                if (croppedTimer < 0)
                    croppedTimer = 0;

                float t = croppedTimer / croppedDuration;
                t = Mathf.Clamp(t, 0, 1);
                t = Mathf.SmoothStep(0, 1, t);

                var camStart = _player.Camera.GlobalTransform;
                Self.CutsceneCam.GlobalTransform = camStart.InterpolateWith(
                    _camTarget.GlobalTransform,
                    t);
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

            private Node3D CamTarget()
            {
                var player = Self.Player;
                var cam = player.Camera;
                var rightPoint = player.FairyKissCamRightPoint;
                var leftPoint = player.FairyKissCamLeftPoint;

                float rightDist = cam.GlobalPosition.DistanceTo(rightPoint.GlobalPosition);
                float leftDist = cam.GlobalPosition.DistanceTo(leftPoint.GlobalPosition);

                return rightDist < leftDist
                    ? rightPoint
                    : leftPoint;
            }
        }

        private class FlyingToPlayer : State<Fairy>
        {
            private const float Duration = 0.5f;

            private Transform3D _start;

            private float _timer;

            public override void OnStateEntered()
            {
                Self.SetPausedForCutscene(true);
                Self.Animator.Play("Hovering", 0.1f);
                Self.Player.Animator.Play("Idle");
                Self.JingleSound.Play();

                _start = Self.Model.GlobalTransform;
                _timer = 0;
            }

            public override void OnStateExited()
            {
                Self.SetPausedForCutscene(false);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;
                float t = _timer / Duration;
                t = Mathf.SmoothStep(0, 1, t);

                var player = Self.Player;

                var target = player.FairyKissPoint.GlobalTransform;
                Self.Model.GlobalTransform = _start.InterpolateWith(target, t);

                if (_timer > Duration)
                {
                    Self.Model.GlobalTransform = target;
                    ChangeState<KissingPlayer>();
                }
            }
        }

        private class KissingPlayer : State<Fairy>
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
            }

            public override void _PhysicsProcess(double deltaD)
            {
                if (!Self.Animator.IsPlaying())
                    ChangeState<RestoringCamera>();
            }
        }

        private class RestoringCamera : State<Fairy>
        {
            private const float Duration = 0.75f;
            private float _timer;
            private Transform3D _start;

            public override void OnStateEntered()
            {
                _timer = 0;
                _start = Self.CutsceneCam.GlobalTransform;
                Self.Animator.Play("FlyAwayHigh");
            }

            public override void OnStateExited()
            {
                Self.Player.Camera.MakeCurrent();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                float t = _timer / Duration;
                t = Mathf.SmoothStep(0, 1, t);

                var end = Self.Player.Camera.GlobalTransform;
                Self.CutsceneCam.GlobalTransform = _start.InterpolateWith(end, t);

                if (_timer > Duration)
                    ChangeState<Rescued>();
            }
        }

        private class QuickRescue : State<Fairy>
        {
            private const float MoveToCameraDuration = 0.5f;

            private Transform3D _initialOffsetFromCamera;
            private Transform3D _targetOffsetFromCamera;

            private float _timer;

            public override void OnStateEntered()
            {
                SaveFile.Current.CurrentMapProgress.CollectedFairies.Add(Self.GetSaveKey());

                Self.Glass.Visible = false;
                Self.GlassParticles.Emitting = true;
                Self.ShatterSound.Play();

                Self.Animator.Play("Shatter");
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

        private class Rescued : State<Fairy>
        {
            public override void OnStateEntered()
            {
                Self.Visible = false;
            }
        }
    }
}