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
            player.ResetPhysicsInterpolation();

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
            protected Vent _vent => _stateMachine.GetParent<Vent>();
            protected Player _player => _vent._player;
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
                _vent._animator.Play("PlayerEntering");

                _player.ChangeState<PlayerManhandledState>();
                _player.Animator.Play("VentEnter", EnterTweenDuration);
                _player.Velocity = Vector3.Zero;

                _playerStart = _player.GlobalTransform;
                _playerTimer = 0;

                _camTimer = 0;
                _cameraStart = _player.Camera.GlobalTransform;
                _cameraEnd = ClosestCameraPoint().GlobalTransform;

                _vent._cutsceneCam.GlobalTransform = _cameraStart;
                _vent._cutsceneCam.MakeCurrent();
                _vent._cutsceneCam.ResetPhysicsInterpolation();

                Node3D ClosestCameraPoint()
                {
                    float leftDist = _vent._cameraPointLeft
                        .GlobalPosition
                        .DistanceTo(_player.Camera.GlobalPosition);

                    float rightDist = _vent._cameraPointRight
                        .GlobalPosition
                        .DistanceTo(_player.Camera.GlobalPosition);

                    return leftDist < rightDist
                        ? _vent._cameraPointLeft
                        : _vent._cameraPointRight;
                }
            }

            public override void OnStateExited()
            {
                _player.Camera.MakeCurrent();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _playerTimer += (float)deltaD;
                _camTimer += (float)deltaD;

                float camT = Mathf.Min(_camTimer / EnterCameraMoveDuration, 1);
                _vent._cutsceneCam.GlobalTransform = _cameraStart.InterpolateWith(
                    _cameraEnd,
                    camT
                );

                float playerT = Mathf.Min(_playerTimer / EnterTweenDuration, 1);
                _player.GlobalTransform = _playerStart.InterpolateWith(
                    _vent.GlobalTransform,
                    playerT
                );

                if (!_player.Animator.IsPlaying())
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
                _camStart = _vent._cutsceneCam.GlobalTransform;

                _player.GlobalTransform = _vent._targetVent._spawnPoint.GlobalTransform;
                _player.ResetPhysicsInterpolation();

                _player.Camera.OrbitPitchRad = 0;
                _player.Camera.OrbitYawRad = _player.YawRad + Mathf.DegToRad(180);
                _player.Camera.ResetPhysicsInterpolation();

                _player.Visible = false;
                _vent._cutsceneCam.MakeCurrent();
            }

            public override void OnStateExited()
            {
                _player.Visible = true;
                _player.Camera.MakeCurrent();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer +=(float)deltaD;

                float t = Mathf.Min(_timer / MoveDuration, 1);
                _vent._cutsceneCam.GlobalTransform = _camStart.InterpolateWith(
                    _player.Camera.GlobalTransform,
                    t
                );

                if (_timer >= MoveDuration)
                {
                    _vent._targetVent.ExitFrom(_player);
                    ChangeState<Idle>();
                }
            }
        }
    }
}