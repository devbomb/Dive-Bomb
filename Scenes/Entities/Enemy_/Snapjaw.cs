using Godot;
using System;

namespace FastDragon
{
    public partial class Snapjaw : Node3D, IGemContainer
    {
        [Export] public GemColor GemColor { get; set; } = GemColor.Red;
        [Export] public string CycleId = null;
        [Export] public double CycleOffset;

        private RayCast3D _floorDetector => GetNode<RayCast3D>("%FloorDetector");
        private AnimationTree _animator => GetNode<AnimationTree>("%AnimationTree");
        private Node3D _model => GetNode<Node3D>("%Model");
        private readonly StateMachine _stateMachine = new StateMachine();

        private Vector3 _targetPos;
        private Vector3 _floorPos;
        private bool _foundFloorPos = false;

        public override void _Ready()
        {
            AddChild(_stateMachine);
            SignalBus.Instance.LevelReset += Reset;

            _targetPos = GlobalPosition;

            // HACK: We can't do the raycast in _Ready() because the floor may
            // not have been added to the tree yet.  Instead, we'll wait until
            // the first frame to find it.
            Visible = false;
            _floorPos = GlobalPosition;
            Callable.From(() =>
            {
                Visible = true;

                _floorDetector.ForceRaycastUpdate();
                _floorPos = _floorDetector.GetCollisionPoint();
                _floorPos.Y += 1;

                _floorDetector.Enabled = false;
                Reset();
            }).CallDeferred();
        }

        private void Reset()
        {
            if (string.IsNullOrEmpty(CycleId))
                _stateMachine.ChangeState<WaitingForCycleOffset>();
            else
                _stateMachine.ChangeState<WaitingForCycleStart>();
        }

        public void OnBroken()
        {
            _stateMachine.ChangeState<DeathFlipping>();
        }

        private void MoveToWatchingPosition()
        {
            _animator.PlayState("Watch");
            _animator.Advance(0);

            GlobalPosition = _floorPos;
            this.ResetPhysicsInterpolation3D();
        }

        private void FacePlayer()
        {
            var player = GetTree().FindNode<Player>();
            if (player == null)
                return;

            GlobalRotation = GlobalPosition
                    .DirectionTo(player.GlobalPosition)
                    .Flattened()
                    .Normalized()
                    .ForwardToEulerAnglesRad();
        }

        private class WaitingForCycleStart : State<Snapjaw>
        {
            public override void OnStateEntered()
            {
                SignalBus.Instance.CycleStarted += OnCycleStarted;
                Self.MoveToWatchingPosition();
            }

            public override void OnStateExited()
            {
                SignalBus.Instance.CycleStarted -= OnCycleStarted;
            }

            private void OnCycleStarted(string cycleId)
            {
                if (cycleId == Self.CycleId)
                    ChangeState<WaitingForCycleOffset>();
            }
        }

        private class WaitingForCycleOffset : State<Snapjaw>
        {
            private double _timer;

            public override void OnStateEntered()
            {
                Self.MoveToWatchingPosition();
                _timer = Self.CycleOffset;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;
                if (_timer <= 0)
                    ChangeState<Watching>();
            }
        }

        private class Watching : State<Snapjaw>
        {
            private const double Duration = 2;

            private double _timer;

            public override void OnStateEntered()
            {
                Self.MoveToWatchingPosition();
                _timer = Duration;
            }

            public override void _PhysicsProcess(double delta)
            {
                Self.GlobalPosition = Self._floorPos;
                Self.ResetPhysicsInterpolation3D();

                Self.FacePlayer();

                _timer -= delta;
                if (_timer <= 0)
                    ChangeState<WindingUp>();
            }
        }

        private class WindingUp : State<Snapjaw>
        {
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = Self._animator.GetAnimPlayer().GetAnimation("WindUp").Length;
                Self.MoveToWatchingPosition();

                Self.GlobalPosition = Self._floorPos;
                Self.ResetPhysicsInterpolation3D();

                Self._animator.PlayState("WindUp");
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;
                Self.GlobalPosition = Self._floorPos;
                Self.ResetPhysicsInterpolation3D();

                if (_timer <= 0)
                    ChangeState<Attacking>();
            }
        }

        private class Attacking : State<Snapjaw>
        {
            private double _duration;
            private double _timer;

            public override void OnStateEntered()
            {
                _duration = Self._animator.GetAnimPlayer().GetAnimation("Attack").Length;

                _timer = 0;
                Self.GlobalPosition = Self._floorPos;
                Self.ResetPhysicsInterpolation3D();
                Self.FacePlayer();

                Self._animator.PlayState("Attack");
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;
                float t = (float)(_timer / _duration);
                t = 1f - Mathf.Pow(t - 1, 4);

                Self.GlobalPosition = Self._floorPos.Lerp(Self._targetPos, t);

                if (_timer >= _duration)
                    ChangeState<Hovering>();
            }
        }

        private class Hovering : State<Snapjaw>
        {
            private const double Duration = 0.5;

            private double _timer;

            public override void OnStateEntered()
            {
                _timer = Duration;
                Self.GlobalPosition = Self._targetPos;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;
                if (_timer <= 0)
                    ChangeState<Falling>();
            }
        }

        private class Falling : State<Snapjaw>
        {
            private const double Duration = 0.5;

            private double _timer;

            public override void OnStateEntered()
            {
                _timer = 0;
                Self.GlobalPosition = Self._targetPos;
                Self._animator.GetAnimPlayer().SpeedScale = (float)(1.0 / Duration);

                Self._animator.PlayState("Fall");
            }

            public override void OnStateExited()
            {
                Self._animator.GetAnimPlayer().SpeedScale = 1;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;
                float t = (float)(_timer / Duration);
                t = Mathf.Pow(t, 2);

                Self.GlobalPosition = Self._targetPos.Lerp(Self._floorPos, t);

                if (_timer >= Duration)
                    ChangeState<Watching>();
            }
        }

        private class DeathFlipping : State<Snapjaw>
        {
            private double _duration;
            private double _timer;
            private Vector3 _startPos;

            public override void OnStateEntered()
            {
                _duration = Self._animator.GetAnimPlayer().GetAnimation("DeathFlip").Length;
                _timer = 0;
                _startPos = Self.GlobalPosition;

                Self._animator.PlayState("DeathFlip");
                Self.FacePlayer();
                Self.ResetPhysicsInterpolation3D();
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;

                float t = (float)(2 * _timer / _duration);
                t = Mathf.Min(t, 1f);
                Self.GlobalPosition = _startPos.Lerp(Self._targetPos, t);

                if (_timer >= _duration)
                    ChangeState<DeathFalling>();
            }
        }

        private class DeathFalling : State<Snapjaw>
        {
            private const double Duration = 0.3;

            private double _timer;

            public override void OnStateEntered()
            {
                _timer = 0;
                Self.GlobalPosition = Self._targetPos;
                Self._animator.Set("parameters/DeathFall/TimeScale/scale", (float)1.0 / Duration);

                Self._animator.PlayState("DeathFall");
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;
                float t = (float)(_timer / Duration);
                t = Mathf.Pow(t, 2);

                Self.GlobalPosition = Self._targetPos.Lerp(Self._floorPos, t);

                if (_timer >= Duration)
                    ChangeState<Dead>();
            }
        }

        private class Dead : State<Snapjaw>
        {
            public override void OnStateEntered()
            {
                Self._animator.PlayState("RESET");
                Self._model.Visible = false;
            }

            public override void OnStateExited()
            {
                Self._model.Visible = true;
            }
        }
    }
}
