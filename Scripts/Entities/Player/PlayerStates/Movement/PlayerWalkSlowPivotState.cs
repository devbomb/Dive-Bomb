using Godot;

namespace FastDragon
{
    public partial class PlayerWalkSlowPivotState : PlayerState
    {
        public override bool AllowFlaming => false;
        private float _targetYawRad;
        private float _decel;

        public override void OnStateEntered()
        {
            _targetYawRad = LeftStick3D().ForwardToEulerAnglesRad().Y;
            _decel = _player.FSpeed / Player.SlowPivot.MaxSkidDuration;
            _decel = Mathf.Max(_decel, Player.SlowPivot.MinDecel);
            _player.Animator.Play("Skid", 0.25);
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                _player.ChangeState<PlayerWalkJumpState>();
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            // Skid down to a stop
            _player.FSpeed = Mathf.MoveToward(
                _player.FSpeed,
                0,
                _decel * delta
            );
            bool doneSkidding = Mathf.IsZeroApprox(_player.FSpeed);

            // Turn to the target direction
            if (doneSkidding)
            {
                _player.YawRad = AngleMath.MoveToward(
                    _player.YawRad,
                    _targetYawRad,
                    Mathf.DegToRad(Player.SlowPivot.RotSpeedDeg) * delta
                );
            }
            bool doneTurning = Mathf.IsEqualApprox(_player.YawRad, _targetYawRad);

            _player.MoveAndSlide();

            // Flop if we leave the ground
            if (!_player.IsOnFloor())
            {
                _player.ChangeState<PlayerFlopState>();
                return;
            }

            // Begin walking once we reach the target direction
            if (doneSkidding && doneTurning)
            {
                _player.ChangeState<PlayerWalkState>();
                return;
            }
        }
    }
}