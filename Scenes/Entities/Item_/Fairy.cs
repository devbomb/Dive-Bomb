using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class Fairy : StaticBody3D, IBreakable
    {
        private readonly StateMachine _stateMachine = new StateMachine(typeof(FairyState));
        private Transform3D _initialModelPos;

        private AnimationPlayer Animator => GetNode<AnimationPlayer>("%AnimationPlayer");
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
            Shatter();
        }

        private void Shatter()
        {
            bool isTimeTrial = GetTree().Root.FindNode<TimeTrialManager>()?.IsTimeTrialMode ?? false;

            if (isTimeTrial)
                _stateMachine.ChangeState<QuickRescue>();
            else
                _stateMachine.ChangeState<Shattering>();
        }

        private void SetPausedForCutscene(bool paused)
        {
            GetTree().Paused = paused;

            ProcessMode = paused
                ? ProcessModeEnum.Always
                : ProcessModeEnum.Inherit;

            Player.ProcessMode = ProcessMode;
        }

        private partial class Idle : FairyState
        {
            public override void OnStateEntered()
            {
                _fairy.Player = GetTree().FindNode<Player>();
                _fairy.Visible = true;
                _fairy.Glass.Visible = true;
                _fairy.Animator.Play("PoundingOnGlass");

                _fairy.CollisionShape.Disabled = false;
            }

            public override void OnStateExited()
            {
                _fairy.CollisionShape.Disabled = true;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _fairy.Model.GlobalRotation = _fairy.GlobalPosition
                    .DirectionTo(_fairy.Player.GlobalPosition)
                    .Flattened()
                    .Normalized()
                    .ForwardToEulerAnglesRad();
            }
        }

        private partial class Shattering : FairyState
        {
            private const float TimeScale = 0.5f;

            private static float PlayerJumpSpeed => Player.Jump.InitVSpeed;
            private static float PlayerGravity => Player.Jump.ShortHopGravity;

            private Player _player => _fairy.Player;

            private bool _playerLanded;
            private float _timer;

            public override void OnStateEntered()
            {
                SaveFile.Current.CurrentMapProgress.CollectedFairies.Add(_fairy.GetSaveKey());

                // Pause the game (except the player and fairy) during the
                // cutscene to prevent the player from getting hit by enemies.
                // Don't worry, the pause menu won't open if the game is already
                // paused by something else.
                _fairy.SetPausedForCutscene(true);

                // Hijack control of the player.  We're going to manipulate them
                // for the cutscene.
                _fairy.Player.ChangeState<PlayerManhandledState>();
                _player.Velocity = Vector3.Up * PlayerJumpSpeed;
                _player.Velocity += _player.GlobalForward() * -1;
                _playerLanded = false;

                // Krrsssh!!!  Shatter the glass!
                _fairy.Glass.Visible = false;
                _fairy.GlassParticles.Emitting = true;
                _fairy.Animator.Play("Shatter");

                Engine.TimeScale = TimeScale;
            }

            public override void OnStateExited()
            {
                _fairy.SetPausedForCutscene(false);
                Engine.TimeScale = 1;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _timer += delta;

                ApplyGravityToPlayer(delta);

                if (_playerLanded && !_player.Animator.IsPlaying())
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
        }

        private partial class FlyingToPlayer : FairyState
        {
            private const float Duration = 1;

            private Transform3D _start;

            private float _timer;

            public override void OnStateEntered()
            {
                _fairy.SetPausedForCutscene(true);
                _fairy.Animator.Play("Hovering", 0.1f);
                _fairy.Player.Animator.Play("Idle");

                _start = _fairy.Model.GlobalTransform;

                _fairy.CutsceneCam.GlobalTransform = _fairy.Player.Camera.GlobalTransform;
                _fairy.CutsceneCam.MakeCurrent();
                _fairy.CutsceneCam.ResetPhysicsInterpolation3D();

                _timer = 0;
            }

            public override void OnStateExited()
            {
                _fairy.SetPausedForCutscene(false);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;
                float t = _timer / Duration;

                var player = _fairy.Player;

                var target = player.FairyKissPoint.GlobalTransform;
                var camStart = player.Camera.GlobalTransform;
                var camTarget = CamTarget();

                _fairy.Model.GlobalTransform = _start.InterpolateWith(target, t);
                _fairy.CutsceneCam.GlobalTransform = camStart.InterpolateWith(camTarget, t);

                if (_timer > Duration)
                {
                    _fairy.Model.GlobalTransform = target;
                    _fairy.CutsceneCam.GlobalTransform = camTarget;

                    ChangeState<KissingPlayer>();
                }
            }

            private Transform3D CamTarget()
            {
                var player = _fairy.Player;
                var cam = player.Camera;
                var rightPoint = player.FairyKissCamRightPoint;
                var leftPoint = player.FairyKissCamLeftPoint;

                float rightDist = cam.GlobalPosition.DistanceTo(rightPoint.GlobalPosition);
                float leftDist = cam.GlobalPosition.DistanceTo(leftPoint.GlobalPosition);

                return rightDist < leftDist
                    ? rightPoint.GlobalTransform
                    : leftPoint.GlobalTransform;
            }
        }

        private partial class KissingPlayer : FairyState
        {
            public override void OnStateEntered()
            {
                _fairy.SetPausedForCutscene(true);
                _fairy.Animator.Play("Kiss", 0.3f);
                _fairy.Animator.Queue("FlyAway");
            }

            public override void OnStateExited()
            {
                _fairy.SetPausedForCutscene(false);
                _fairy.Player.ChangeState<PlayerWalkState>();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                if (!_fairy.Animator.IsPlaying())
                    ChangeState<RestoringCamera>();
            }
        }

        private partial class RestoringCamera : FairyState
        {
            private const float Duration = 0.5f;
            private float _timer;
            private Transform3D _start;

            public override void OnStateEntered()
            {
                _timer = 0;
                _start = _fairy.CutsceneCam.GlobalTransform;
            }

            public override void OnStateExited()
            {
                _fairy.Player.Camera.MakeCurrent();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                float t = _timer / Duration;
                var end = _fairy.Player.Camera.GlobalTransform;
                _fairy.CutsceneCam.GlobalTransform = _start.InterpolateWith(end, t);

                if (_timer > Duration)
                    ChangeState<Rescued>();
            }
        }

        private partial class QuickRescue : FairyState
        {
            private const float MoveToCameraDuration = 0.5f;

            private Transform3D _initialOffsetFromCamera;
            private Transform3D _targetOffsetFromCamera;

            private float _timer;

            public override void OnStateEntered()
            {
                SaveFile.Current.CurrentMapProgress.CollectedFairies.Add(_fairy.GetSaveKey());

                _fairy.Glass.Visible = false;
                _fairy.GlassParticles.Emitting = true;

                _fairy.Animator.Play("Shatter");
                _fairy.Animator.Queue("FlyAway");

                _initialOffsetFromCamera = SaveOffsetFromCamera();
                _targetOffsetFromCamera = _fairy.Player.Camera.TimeTrialFairyRescuePos.Transform;

                _timer = 0;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;
                float t = Mathf.Min(_timer / MoveToCameraDuration, 1);
                t = MathUtils.LerpSinusoidal(0, 1, t);

                var offsetFromCamera = _initialOffsetFromCamera.InterpolateWith(_targetOffsetFromCamera, t);
                var camera = GetTree().Root.GetCamera3D();
                _fairy.Model.GlobalTransform = camera.GlobalTransform * offsetFromCamera;

                if (!_fairy.Animator.IsPlaying())
                    ChangeState<Rescued>();
            }

            private Transform3D SaveOffsetFromCamera()
            {
                var camera = GetTree().Root.GetCamera3D();
                return camera.GlobalTransform.AffineInverse() * _fairy.Model.GlobalTransform;
            }
        }

        private partial class Rescued : FairyState
        {
            public override void OnStateEntered()
            {
                _fairy.Visible = false;
            }
        }

        private abstract partial class FairyState : State
        {
            protected Fairy _fairy => _stateMachine.GetParent<Fairy>();
        }
    }
}