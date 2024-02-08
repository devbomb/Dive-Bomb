using Godot;

namespace FastDragon
{
    public partial class PlayerKickState : PlayerState
    {
        private float _timer;

        public override void OnStateEntered()
        {
            _timer = Player.Kick.Duration;
            _player.VSpeed = Player.Kick.InitVSpeed;
            _player.Animator.Play("Kick");
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev) && _player.IsOnFloor())
            {
                _player.ChangeState<PlayerWalkJumpState>();
                return;
            }

            if (InputService.RollJustPressed(ev))
            {
                if (_player.IsOnFloor())
                    _player.ChangeState<PlayerRollState>();
                else
                    _player.ChangeState<PlayerDiveState>();

                return;
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            ApplyKickHitbox();

            ApplyGravity(delta);
            StrafeWithLeftStick(Player.Walk.Speed, Player.Walk.Accel, delta);

            if (!_player.Velocity.Flattened().IsZeroApprox())
                RotateInstantlyTowardVelocity();

            _player.MoveAndSlide();

            _timer -= delta;
            if (_timer <= 0)
            {
                if (_player.IsOnFloor())
                {
                    _player.ChangeState<PlayerWalkState>();
                }
                else
                {
                    _player.ChangeState<PlayerKickFlopState>();
                }
            }
        }

        private void ApplyKickHitbox()
        {
            var bodies = _player.KickHitbox.GetOverlappingBodies();
            var areas = _player.KickHitbox.GetOverlappingAreas();

            foreach (var body in bodies)
            {
                if (body is IKickable f)
                {
                    f.OnKicked();
                }
            }

            foreach (var area in areas)
            {
                if (area is IKickable f)
                {
                    f.OnKicked();
                }
            }
        }
    }
}