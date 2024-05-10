using System;
using Godot;

namespace FastDragon
{
    public partial class SeamonsterBoss : CharacterBody3D
    {
        [ExportCategory("Timings")]
        [Export] public float SubmergedDuration = 1;
        [Export] public float SubmergingDuration = 0.5f;
        [Export] public float SurfacingDuration = 0.5f;
        [Export] public float IdleDuration = 1f;

        [ExportCategory("Points")]
        [Export] public float SubmergeDepth = -14;
        [Export] public Node3D InitialSpawnPoint;
        [Export] public Node3D CameraFixPoint;
        [Export] public Node3D[] SpawnPoints = new Node3D[0];

        private BreakableArea3D _weakPoint => GetNode<BreakableArea3D>("%WeakPoint");

        private readonly StateMachine _stateMachine = new StateMachine(typeof(SeamonsterBossState));
        private readonly Random _rng = new Random();

        private Transform3D _currentSpawnPos;


        public override void _Ready()
        {
            AddChild(_stateMachine);

            SignalBus.Instance.LevelReset += Respawn;

            // Defer respawning to ensure the player is ready, since we'll be
            // hijacking the camera.
            CallDeferred(nameof(Respawn));
        }

        public void Respawn()
        {
            _currentSpawnPos = InitialSpawnPoint.GlobalTransform;
            _stateMachine.ChangeState<Submerged>();

            // Hijack the camera
            GetTree().FindNode<Player>().Camera.FixPosition(CameraFixPoint.GlobalTransform);
        }

        private void RandomizeSpawnPoint()
        {
            _currentSpawnPos = _rng.PickFrom(SpawnPoints).GlobalTransform;
        }

        private abstract partial class SeamonsterBossState : State
        {
            protected SeamonsterBoss _self => _stateMachine.GetParent<SeamonsterBoss>();
        }

        private partial class Submerged : SeamonsterBossState
        {
            private float _timer;

            public override void OnStateEntered()
            {
                _self.GlobalTransform = _self._currentSpawnPos;
                _self.GlobalPosition += Vector3.Up * _self.SubmergeDepth;
                _self.ResetPhysicsInterpolation();

                _timer = _self.SubmergedDuration;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer -= (float)deltaD;

                if (_timer <= 0)
                    ChangeState<Surfacing>();
            }
        }

        private partial class Surfacing : SeamonsterBossState
        {
            private Transform3D _initialPos;

            private float _timer;

            public override void OnStateEntered()
            {
                _timer = 0;
                _initialPos = _self.GlobalTransform;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                float t = Mathf.Min(_timer / _self.SurfacingDuration, 1);
                _self.GlobalTransform = _initialPos.InterpolateWith(_self._currentSpawnPos, t);

                if (_timer >= _self.SurfacingDuration)
                {
                    _self.GlobalTransform = _self._currentSpawnPos;
                    ChangeState<Idle>();
                }

            }
        }

        private partial class Idle : SeamonsterBossState
        {
            private float _timer;

            public override void OnStateEntered()
            {
                _timer = _self.IdleDuration;
                _self._weakPoint.Visible = true;
                _self._weakPoint.Disabled = false;
                _self._weakPoint.Broken += OnBroken;
            }

            public override void OnStateExited()
            {
                _self._weakPoint.Disabled = true;
                _self._weakPoint.Broken -= OnBroken;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer -= (float)deltaD;
                if (_timer <= 0)
                    ChangeState<Submerging>();
            }

            private void OnBroken()
            {
                _self._weakPoint.Visible = false;
                ChangeState<Submerging>();
            }
        }

        private partial class Submerging : SeamonsterBossState
        {
            private Transform3D _initialPos;
            private Transform3D _targetPos;

            private float _timer;

            public override void OnStateEntered()
            {
                _timer = 0;
                _initialPos = _self.GlobalTransform;
                _targetPos = _self._currentSpawnPos.Translated(Vector3.Up * _self.SubmergeDepth);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                float t = Mathf.Min(_timer / _self.SubmergingDuration, 1);
                _self.GlobalTransform = _initialPos.InterpolateWith(_targetPos, t);

                if (_timer >= _self.SubmergingDuration)
                {
                    _self.RandomizeSpawnPoint();
                    ChangeState<Submerged>();
                }
            }
        }
    }
}