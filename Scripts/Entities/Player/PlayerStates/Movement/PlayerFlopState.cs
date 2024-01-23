using Godot;

namespace FastDragon
{
    public partial class PlayerFlopState : PlayerState
    {
        public override void OnStateEntered()
        {
            _player.Animator.Play("Flop");
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            ApplyGravity(delta, Player.Default.Gravity);
            _player.MoveAndSlide();

            if (_player.IsOnFloor())
            {
                _player.ChangeState<PlayerWalkState>();
                return;
            }

            if (InputService.ChargeHeld)
            {
                _player.ChangeState<PlayerChargeFallState>();
                return;
            }
        }
    }
}