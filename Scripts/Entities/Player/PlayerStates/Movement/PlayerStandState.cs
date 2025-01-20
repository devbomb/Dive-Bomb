using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerStandState : PlayerState
    {
        public override void OnStateEntered()
        {
            _player.Animator.Play("Idle");
        }

        public override void OnStateExited()
        {
            ResetModelPitch();
        }

        public override void _Process(double deltaD)
        {
            float delta = (float)deltaD;

            AngleModelPitchWithGroundSlope(delta);
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                _player.ChangeState<PlayerWalkJumpState>();
                return;
            }

            if (InputService.KickJustPressed(ev))
            {
                _player.ChangeState<PlayerKickState>();
                return;
            }

            if (InputService.RollJustPressed(ev))
            {
                _player.ChangeState<PlayerRollState>();
                return;
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;
            _player.Velocity = _player.Velocity.MoveToward(Vector3.Zero, Player.Walk.Decel * delta);
            _player.MoveAndSlide();

            if (!_player.IsOnFloor())
            {
                _player.ChangeState<PlayerFlopState>();
                return;
            }

            bool isPushingStick = !LeftStick3D().IsZeroApprox();
            if (isPushingStick)
            {
                // Instantly pivot when starting from a stop, instead of
                // gradually turning like normal.  Otherwise, you'd walk in a
                // wide circle like in Mario 64.
                RotateInstantlyTowardLeftStick();
                _player.ChangeState<PlayerWalkState>();
                return;
            }
        }
    }
}

