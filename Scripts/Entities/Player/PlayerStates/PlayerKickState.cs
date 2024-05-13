using Godot;

namespace FastDragon
{
    public partial class PlayerKickState : PlayerState
    {
        private const float CameraShakeMagnitude = 0.25f;
        private const float CameraShakeFrequency = 15;
        private const float CameraShakeDuration = 0.25f;


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
                if (body is IBreakable b)
                {
                    b.OnKicked();

                    if (b.VulnerableToKick)
                        Break(b);
                }
            }

            foreach (var area in areas)
            {
                if (area is IBreakable b)
                {
                    b.OnKicked();

                    if (b.VulnerableToKick)
                        Break(b);
                }
            }
        }

        private void Break(IBreakable b)
        {
            b.OnBroken();
            _player.Camera.Shake(
                CameraShakeMagnitude,
                CameraShakeFrequency,
                CameraShakeDuration
            );
        }
    }
}