using Godot;
using System;

namespace FastDragon
{
    public partial class Snapjaw : Node3D
    {
        private RayCast3D _floorDetector => GetNode<RayCast3D>("%FloorDetector");
        private AnimationTree _animator => GetNode<AnimationTree>("%AnimationTree");
        private readonly StateMachine _stateMachine = new StateMachine(typeof(SnapjawState));

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
            }).CallDeferred();

            Reset();
        }

        private void Reset()
        {
            _stateMachine.ChangeState<Watching>();
        }

        public void OnBroken()
        {
            _stateMachine.ChangeState<Dead>();
        }

        private abstract partial class SnapjawState : State
        {
            protected Snapjaw _self => _stateMachine.GetParent<Snapjaw>();
        }

        private partial class Watching : SnapjawState
        {
            private const double Duration = 2;

            private double _timer;

            public override void OnStateEntered()
            {
                _self._animator.PlayState("Watch");
                _self._animator.Advance(0);
                _timer = Duration;

                _self.GlobalPosition = _self._floorPos;
                _self.ResetPhysicsInterpolation3D();
            }

            public override void _PhysicsProcess(double delta)
            {
                _self.GlobalPosition = _self._floorPos;
                _self.ResetPhysicsInterpolation3D();

                _timer -= delta;
                if (_timer <= 0)
                    ChangeState<Attacking>();
            }
        }

        private partial class Attacking : SnapjawState
        {
            private double _duration;
            private double _timer;

            public override void OnStateEntered()
            {
                _duration = _self._animator.GetAnimPlayer().GetAnimation("Attack").Length;

                _timer = 0;
                _self.GlobalPosition = _self._floorPos;
                _self.ResetPhysicsInterpolation3D();

                _self._animator.PlayState("Attack");
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;
                float t = (float)(_timer / _duration);
                t = 1 - ((t - 1) * (t - 1));

                _self.GlobalPosition = _self._floorPos.Lerp(_self._targetPos, t);

                if (_timer >= _duration)
                    ChangeState<Hovering>();
            }
        }

        private partial class Hovering : SnapjawState
        {
            private const double Duration = 0.5;

            private double _timer;

            public override void OnStateEntered()
            {
                _timer = Duration;
                _self.GlobalPosition = _self._targetPos;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;
                if (_timer <= 0)
                    ChangeState<Falling>();
            }
        }

        private partial class Falling : SnapjawState
        {
            private const double Duration = 0.5;

            private double _timer;

            public override void OnStateEntered()
            {
                _timer = 0;
                _self.GlobalPosition = _self._targetPos;
                _self._animator.GetAnimPlayer().SpeedScale = (float)(1.0 / Duration);

                _self._animator.PlayState("Fall");
            }

            public override void OnStateExited()
            {
                _self._animator.GetAnimPlayer().SpeedScale = 1;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;
                float t = (float)(_timer / Duration);
                t = Mathf.Pow(t, 2);

                _self.GlobalPosition = _self._targetPos.Lerp(_self._floorPos, t);

                if (_timer >= Duration)
                    ChangeState<Watching>();
            }
        }

        private partial class Dead : SnapjawState
        {
            public override void OnStateEntered()
            {
                _self._animator.PlayState("RESET");
                _self.Visible = false;
            }

            public override void OnStateExited()
            {
                _self.Visible = true;
            }
        }
    }
}
