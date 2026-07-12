using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerStandState : PlayerState
    {
        private float _boundJumpWindowTimer;

        public override void OnStateEntered(IState oldState)
        {
            Self.Animator.Play("Idle");

            bool canBound = (oldState as PlayerState)?.CanBoundAfterLanding ?? false;
            _boundJumpWindowTimer = canBound
                ? Player.BoundJump.TimeWindow
                : 0;

            // Let the player jump if they pressed the button a little bit too
            // early.
            //
            // Note that this does NOT update the last safe grounded position.
            // That's intentional!
            // The player can use this to delay setting their last safe position,
            // effectively using it as a "remote teleport" by jumping into the
            // water.
            //
            // TODO: Find some way to de-duplicate all of the bound jump logic
            // between this class and PlayerWalkState.  And probably other
            // redundant code, too.
            if (Self.EarlyJumpBufferTimer > 0)
            {
                Jump();
            }
        }

        public override void OnStateExited()
        {
            ResetModelPitch();
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                Jump();
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

            _boundJumpWindowTimer -= delta;

            Self.LocalVelocity = Self.LocalVelocity.MoveToward(Vector3.Zero, Player.Walk.Decel * delta);
            Self.MoveAndSlide();
            Self.SafeGround.UpdateLastSafeGroundPos();

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

        private void Jump()
        {
            if (_boundJumpWindowTimer > 0)
                Self.ChangeState<PlayerBoundJumpState>();
            else
                Self.ChangeState<PlayerWalkJumpState>();
        }
    }
}

