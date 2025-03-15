using Godot;

namespace FastDragon
{
    public partial class PlayerLedgeClimbState : PlayerState
    {
        private float _endHeight;

        public override void OnStateEntered()
        {
            float climbAnimLen = _player.Animator.GetAnimation("Roll").Length;
            _player.Animator.Play(
                "Roll",
                customBlend: 0.125f,
                customSpeed: climbAnimLen / 0.5f
            );

            _player.VSpeed = Player.LedgeClimb.InitVSpeed;
            _player.FSpeed = Player.LedgeClimb.FSpeed;

            _endHeight = _player.LedgeGrabPoint.GlobalPosition.Y;
        }

        public override void OnStateExited()
        {
            _player.Animator.Play("RESET", customBlend: 0);
            _player.Animator.Advance(0);
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            if (_player.GlobalPosition.Y < _endHeight)
                _player.FSpeed = Player.LedgeClimb.FSpeed;
            else
            {
                RotateTowardLeftStick(Player.Jump.RotSpeedRad, delta);
                AccelerateWithLeftStick(
                    Player.Jump.MaxFSpeed,
                    Player.Jump.StrafeAccel,
                    delta
                );
            }

            ApplyGravity(delta, Player.LedgeClimb.Gravity);
            _player.MoveAndSlide();

            if (_player.IsOnFloor())
                _player.ChangeState<PlayerWalkState>();
        }
    }
}