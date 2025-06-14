using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class Vent : Node3D
    {
        private const float CooldownDuration = 1;
        private const float EnterCameraMoveDuration = 0.3f;
        private const float EnterTweenDuration = 0.2f;
        private const float MoveDuration = 3;

        [Export] public string VentId;
        [Export] public string TargetVentId;

        private readonly StateMachine _stateMachine = new StateMachine(typeof(VentState));

        private Node3D _spawnPoint => GetNode<Node3D>("%SpawnPoint");
        private Node3D _cameraPointLeft => GetNode<Node3D>("%CameraPointLeft");
        private Node3D _cameraPointRight => GetNode<Node3D>("%CameraPointRight");

        private Camera3D _cutsceneCam => GetNode<Camera3D>("%CutsceneCam");
        private AnimationPlayer _animator => GetNode<AnimationPlayer>("%AnimationPlayer");
        private AudioStreamPlayer _crawlSound => GetNode<AudioStreamPlayer>("%CrawlSound");

        private float _cooldownTimer;
        private Vent _targetVent;
        private Player _player;

        public override void _Ready()
        {
            AddChild(_stateMachine);
            SignalBus.Instance.LevelReset += OnLevelReset;
            OnLevelReset();
        }

        private void OnLevelReset()
        {
            _stateMachine.ChangeState<Idle>();
        }

        public void Enter(Player player)
        {
            _player = player;
            _targetVent = GetTree().Root
                    .EnumerateDescendantsOfType<Vent>()
                    .FirstOrDefault(v => v.VentId == TargetVentId);

            _stateMachine.ChangeState<Entering>();
        }

        public void ExitFrom(Player player)
        {
            _cooldownTimer = CooldownDuration;

            _animator.Play("BurstOpen");

            player.GlobalTransform = _spawnPoint.GlobalTransform;
            player.ResetPhysicsInterpolation3D();

            player.ChangeState<PlayerFlopState>();
            player.FSpeed = 3;
            player.VSpeed = 3;
        }

        public void OnAreaEntered(Node body)
        {
            if (_cooldownTimer > 0)
                return;

            if (body is Player player && !(player.CurrentState is PlayerManhandledState))
            {
                Enter(player);
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            if (_cooldownTimer > 0)
                _cooldownTimer -= (float)deltaD;
        }

        private abstract partial class VentState : State
        {
            protected Vent Self => _stateMachine.GetParent<Vent>();
            protected Player Player => Self._player;
        }

        private partial class Idle : VentState {}

        private partial class Entering : VentState
        {
            private Transform3D _playerStart;
            private Transform3D _cameraStart;
            private Transform3D _cameraEnd;

            private float _camTimer;
            private float _playerTimer;

            public override void OnStateEntered()
            {
                Self._animator.Play("PlayerEntering");

                Player.ChangeState<PlayerManhandledState>();
                Player.Animator.Play("VentEnter", EnterTweenDuration);
                Player.Velocity = Vector3.Zero;

                _playerStart = Player.GlobalTransform;
                _playerTimer = 0;

                _camTimer = 0;
                _cameraStart = Player.Camera.GlobalTransform;
                _cameraEnd = ClosestCameraPoint().GlobalTransform;

                Self._cutsceneCam.GlobalTransform = _cameraStart;
                Self._cutsceneCam.MakeCurrent();
                Self._cutsceneCam.ResetPhysicsInterpolation3D();

                Node3D ClosestCameraPoint()
                {
                    float leftDist = Self._cameraPointLeft
                        .GlobalPosition
                        .DistanceTo(Player.Camera.GlobalPosition);

                    float rightDist = Self._cameraPointRight
                        .GlobalPosition
                        .DistanceTo(Player.Camera.GlobalPosition);

                    return leftDist < rightDist
                        ? Self._cameraPointLeft
                        : Self._cameraPointRight;
                }
            }

            public override void OnStateExited()
            {
                Player.Camera.MakeCurrent();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _playerTimer += (float)deltaD;
                _camTimer += (float)deltaD;

                float camT = Mathf.Min(_camTimer / EnterCameraMoveDuration, 1);
                Self._cutsceneCam.GlobalTransform = _cameraStart.InterpolateWith(
                    _cameraEnd,
                    camT
                );

                float playerT = Mathf.Min(_playerTimer / EnterTweenDuration, 1);
                Player.GlobalTransform = _playerStart.InterpolateWith(
                    Self.GlobalTransform,
                    playerT
                );

                if (!Player.Animator.IsPlaying())
                    ChangeState<Moving>();
            }
        }

        private partial class Moving : VentState
        {
            private float _timer;
            private float _camTimer;

            private Transform3D _camStart;

            public override void OnStateEntered()
            {
                _timer = 0;

                _camTimer = 0;
                _camStart = Self._cutsceneCam.GlobalTransform;

                Player.GlobalTransform = Self._targetVent._spawnPoint.GlobalTransform;
                Player.ResetPhysicsInterpolation3D();

                Player.Camera.OrbitPitchRad = 0;
                Player.Camera.OrbitYawRad = Player.YawRad + Mathf.DegToRad(180);
                Player.Camera.ResetPhysicsInterpolation3D();

                Player.Visible = false;
                Self._cutsceneCam.MakeCurrent();

                Self._crawlSound.Play();
            }

            public override void OnStateExited()
            {
                Player.Visible = true;
                Player.Camera.MakeCurrent();
                Self._crawlSound.Stop();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer +=(float)deltaD;

                float t = Mathf.Min(_timer / MoveDuration, 1);
                Self._cutsceneCam.GlobalTransform = _camStart.InterpolateWith(
                    Player.Camera.GlobalTransform,
                    t
                );

                if (_timer >= MoveDuration)
                {
                    Self._targetVent.ExitFrom(Player);
                    ChangeState<Idle>();
                }
            }
        }
    }
}