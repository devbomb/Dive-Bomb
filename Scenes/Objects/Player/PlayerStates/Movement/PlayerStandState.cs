using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerStandState : PlayerState
    {
        public override void OnStateEntered()
        {
            Self.Animator.Play("Idle");
        }

        public override void OnStateExited()
        {
            ResetModelPitch();
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                Self.ChangeState<PlayerWalkJumpState>();
                return;
            }

            if (InputService.KickJustPressed(ev))
            {
                Self.ChangeState<PlayerKickState>();
                return;
            }

            if (InputService.RollJustPressed(ev))
            {
                Self.ChangeState<PlayerRollState>();
                return;
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;
            Self.Velocity = Self.Velocity.MoveToward(Vector3.Zero, Player.Walk.Decel * delta);
            Self.MoveAndSlide();
            UpdateLastSafeGroundPos();

            if (!Self.IsOnFloor())
            {
                Self.ChangeState<PlayerFlopState>();
                return;
            }

            bool isPushingStick = !LeftStick3D().IsZeroApprox();
            if (isPushingStick)
            {
                // Instantly pivot when starting from a stop, instead of
                // gradually turning like normal.  Otherwise, you'd walk in a
                // wide circle like in Mario 64.
                RotateInstantlyTowardLeftStick();
                Self.ChangeState<PlayerWalkState>();
                return;
            }
        }
    }
}

