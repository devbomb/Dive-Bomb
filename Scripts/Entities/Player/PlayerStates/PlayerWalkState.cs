using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerWalkState : PlayerState
    {
        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraFreeState>();
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                if (_player.IsOnFloor())
                {
                    // TODO: jump
                }
                else
                {
                    // TODO: Move this into one of the non-grounded states
                    _player.ChangeState<PlayerGlideState>();
                }
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            WalkControls(Player.Walk.Speed, Player.Walk.Accel, delta);
            ApplyGravity(delta);

            _player.MoveAndSlide();

            // Charge when the button is held
            if (InputService.ChargeHeld)
                _player.ChangeState<PlayerChargeState>();
        }
    }
}

