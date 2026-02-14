using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class FairyGuide : Node3D
    {
        private const float MovementDecayRate = 0.1f;

        [Export] public string FairyId;
        [Export] public float HeadStartMeters = 5;
        [Export] public double MoveFromJarDuration = 1;
        [Export] public Path3D Path;
        [Export] public bool VanishWhenFinished;

        private Player _player;
        private FairyJar _jar;

        private readonly StateMachine _stateMachine = new();

        public FairyGuide()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            Callable.From(() =>
            {
                _player = GetTree().FindNode<Player>();

                if (!string.IsNullOrEmpty(FairyId))
                {
                    _jar = this.FindNodeByTargetName<FairyJar>(FairyId);
                }

                SignalBus.Instance.LevelReset += Reset;
                Reset();

            }).CallDeferred();
        }

        private void Reset()
        {
            _stateMachine.ChangeState<Unrescued>();
        }

        public bool IsAtEnd() => GetFairyProgressMeters() >= (Path.Curve.GetBakedLength() - 3f);

        public float GetPlayerProgressMeters()
        {
            Vector3 playerPosInCurveSpace = Path.ToLocal(_player.GlobalPosition);
            return Path.Curve.GetClosestOffset(playerPosInCurveSpace);
        }

        public float GetFairyProgressMeters()
        {
            Vector3 fairyPosInCurveSpace = Path.ToLocal(GlobalPosition);
            return Path.Curve.GetClosestOffset(fairyPosInCurveSpace);
        }

        private Transform3D SamplePath()
        {
            float progress = GetPlayerProgressMeters() + HeadStartMeters;

            Vector3 pos = Path.Curve.SampleBaked(progress);

            Vector3 rot = Path.Curve.SampleBakedWithRotation(progress).Basis.GetEuler();
            rot.X = 0;
            rot.Z = 0;

            var resultRelativeToPath = Transform3D.Identity
                .Rotated(Vector3.Up, rot.Y)
                .Translated(pos);

            return Path.GlobalTransform * resultRelativeToPath;
        }

        private class Unrescued : State<FairyGuide>
        {
            public override void OnStateEntered()
            {
                Self.Visible = false;
            }

            public override void OnStateExited()
            {
                Self.Visible = true;
            }

            public override void _PhysicsProcess(double delta)
            {
                if (Self._jar.IsReadyForGuide())
                    ChangeState<MovingFromJarToStart>();
            }
        }

        private class MovingFromJarToStart : State<FairyGuide>
        {
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = 0;
                Self.GlobalTransform = Self._jar.Model.GlobalTransform;
                Self.ResetPhysicsInterpolation3D();
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;

                float t = (float)(_timer / Self.MoveFromJarDuration);
                t = Math.Clamp(t, 0, 1);
                t = Mathf.SmoothStep(0, 1, t);

                var jarPos = Self._jar.Model.GlobalTransform;
                var pathPos = Self.SamplePath();
                Self.GlobalTransform = jarPos.InterpolateWith(pathPos, t);

                if (_timer >= Self.MoveFromJarDuration)
                    ChangeState<FollowingPathForward>();
            }
        }

        private class FollowingPathForward : State<FairyGuide>
        {
            private const double MaxIdleTime = 0.5;
            private const double AccelTime = 0.5;

            private float _lastPlayerProgress;
            private double _idleTimer;
            private double _accelTimer;
            private Transform3D _initialPos;

            public override void OnStateEntered()
            {
                _lastPlayerProgress = Self.GetPlayerProgressMeters();
                _idleTimer = MaxIdleTime;
                _accelTimer = 0;
                _initialPos = Self.GlobalTransform;
            }

            public override void _PhysicsProcess(double delta)
            {
                // Move to a point on the path that's a little bit ahead of
                // the player.  Start slow and speed up.
                _accelTimer = Mathf.MoveToward(_accelTimer, AccelTime, delta);
                float t = (float)(_accelTimer / AccelTime);

                var targetPos = _initialPos.InterpolateWith(Self.SamplePath(), t);

                Self.GlobalTransform = Self
                    .GlobalTransform
                    .InterpolateWith(targetPos, MovementDecayRate);

                float playerProgress = Self.GetPlayerProgressMeters();
                float deltaProgress = playerProgress - _lastPlayerProgress;
                _lastPlayerProgress = playerProgress;

                // Patiently wait for the player to catch up if they've gone
                // backwards, or if they've been idling for too long.
                if (deltaProgress < 0 || _idleTimer <= 0)
                {
                    ChangeState<WaitingForPlayerToCatchUp>();
                    return;
                }

                if (deltaProgress == 0)
                    _idleTimer -= delta;
                else
                    _idleTimer = MaxIdleTime;

                // Vanish when we reach the end (if that's enabled)
                if (Self.VanishWhenFinished && Self.IsAtEnd())
                    ChangeState<Vanished>();
            }
        }

        private class WaitingForPlayerToCatchUp : State<FairyGuide>
        {
            /// <summary>
            /// How far backwards along the path the player is allowed to go
            /// before the fairy goes back to get them
            /// </summary>
            private double MaxPlayerRegressionMeters = 4;

            /// <summary>
            /// How long the fairy is willing to wait before going back to get
            /// the player
            /// </summary>
            private const double PatienceSeconds = 1;

            private float _furthestPlayerProgress;
            private double _patienceTimer;

            public override void OnStateEntered()
            {
                _furthestPlayerProgress = Self.GetPlayerProgressMeters();
                _patienceTimer = PatienceSeconds;
            }

            public override void _PhysicsProcess(double delta)
            {
                // Stay in place, but look at the player.
                var targetPos = Self.GlobalTransform
                    .LookingAt(Self._player.GlobalPosition)
                    .WithOnlyYRotation();

                Self.GlobalTransform = Self
                    .GlobalTransform
                    .InterpolateWith(targetPos, MovementDecayRate);

                float playerProgress = Self.GetPlayerProgressMeters();

                if (playerProgress > _furthestPlayerProgress)
                {
                    // Start moving foward if the player catches back up
                    ChangeState<FollowingPathForward>();
                }
                else if (playerProgress < _furthestPlayerProgress - MaxPlayerRegressionMeters)
                {
                    // Go back to get the player if they've gone too far backwards
                    ChangeState<GoingBackToGetPlayer>();
                }
                else
                {
                    // Go back to get the player if the fairy has run out of
                    // patience
                    _patienceTimer -= delta;
                    if (_patienceTimer <= 0)
                        ChangeState<GoingBackToGetPlayer>();
                }
            }
        }

        private class GoingBackToGetPlayer : State<FairyGuide>
        {
            private const double AccelTime = 0.5;

            private float _lastPlayerProgress;
            private double _accelTimer;
            private Transform3D _initialPos;

            public override void OnStateEntered()
            {
                _lastPlayerProgress = Self.GetPlayerProgressMeters();
                _accelTimer = 0;
                _initialPos = Self.GlobalTransform;
            }

            public override void _PhysicsProcess(double delta)
            {
                // Follow the path backwards while looking at the player.
                // Start out slow and speed up.
                _accelTimer = Mathf.MoveToward(_accelTimer, AccelTime, delta);
                float t = (float)(_accelTimer / AccelTime);

                var targetPos = _initialPos
                    .InterpolateWith(Self.SamplePath(), t)
                    .LookingAt(Self._player.GlobalPosition)
                    .WithOnlyYRotation();

                Self.GlobalTransform = Self
                    .GlobalTransform
                    .InterpolateWith(targetPos, MovementDecayRate);

                // If the player is moving forward along the path again, start
                // facing forward again.
                float playerProgress = Self.GetPlayerProgressMeters();
                float deltaProgress = playerProgress - _lastPlayerProgress;
                _lastPlayerProgress = playerProgress;

                if (deltaProgress > 0)
                    ChangeState<FollowingPathForward>();
            }
        }

        private class Vanished : State<FairyGuide>
        {
            public override void OnStateEntered()
            {
                // TODO: Play a custom animation, after the fairy's
                // skeleton/model has been redone.
                Self.Visible = false;
            }

            public override void OnStateExited()
            {
                Self.Visible = true;
            }
        }
    }
}