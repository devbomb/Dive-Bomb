using Godot;

namespace FastDragon
{
    public partial class PlayerWalkSlowPivotState : PlayerState
    {
        public override bool AllowFlaming => false;

        private float _timer;
        private float _startYawRad;
        private float _endYawRad;

        public override void OnStateEntered()
        {
            _timer = 0;
            _startYawRad = _player.YawRad;
            _endYawRad = Transform3D.Identity
                .LookingAt(LeftStick3D(), Vector3.Up)
                .Basis
                .GetEuler()
                .Y;
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
            _timer += delta;

            // Slow down
            float accel = Player.Walk.Speed / Player.Walk.SlowPivotTime;
            _player.Velocity = _player.Velocity.MoveToward(Vector3.Zero, accel * delta);

            // Rotate
            float t = _timer / Player.Walk.SlowPivotTime;
            t = Mathf.Min(t, 1);

            _player.YawRad = Mathf.Lerp(_startYawRad, _endYawRad, t);

            // Move
            _player.MoveAndSlide();

            if (!_player.IsOnFloor())
            {
                _player.ChangeState<PlayerFlopState>();
                return;
            }

            // Go to the walking state when time is up
            if (_timer >= Player.Walk.SlowPivotTime)
            {
                _player.ChangeState<PlayerWalkState>();
                return;
            }
        }
    }
}