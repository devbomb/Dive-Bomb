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

        private readonly StateMachine _stateMachine = new StateMachine(typeof(SeamonsterBossState));

        private Transform3D CurrentSpawnPos;

        private Random _rng = new Random();

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
            CurrentSpawnPos = InitialSpawnPoint.GlobalTransform;
            _stateMachine.ChangeState<Submerged>();

            // Hijack the camera
            GetTree().FindNode<Player>().Camera.FixPosition(CameraFixPoint.GlobalTransform);
        }

        private void RandomizeSpawnPoint()
        {
            CurrentSpawnPos = _rng.PickFrom(SpawnPoints).GlobalTransform;
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
                _self.GlobalTransform = _self.CurrentSpawnPos;
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
                _self.GlobalTransform = _initialPos.InterpolateWith(_self.CurrentSpawnPos, t);

                if (_timer >= _self.SurfacingDuration)
                {
                    _self.GlobalTransform = _self.CurrentSpawnPos;
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
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer -= (float)deltaD;
                if (_timer <= 0)
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
                _targetPos = _self.CurrentSpawnPos.Translated(Vector3.Up * _self.SubmergeDepth);
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