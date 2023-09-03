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
                _player.ChangeState<PlayerWalkJumpState>();
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            WalkControls(
                Player.Walk.Speed,
                Player.Walk.Accel,
                Mathf.DegToRad(Player.Walk.RotSpeedDeg),
                delta
            );

            _player.MoveAndSlide();

            if (InputService.ChargeHeld)
            {
                _player.ChangeState<PlayerChargeState>();
                return;
            }

            if (!_player.IsOnFloor())
            {
                _player.ChangeState<PlayerWalkFallState>();
                return;
            }
        }
    }
}

