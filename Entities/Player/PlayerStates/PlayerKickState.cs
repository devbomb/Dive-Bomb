using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class PlayerKickState : PlayerState
    {
        private float _timer;

        public override void OnStateEntered()
        {
            _timer = Player.Kick.Duration;
            Self.VSpeed = Player.Kick.InitVSpeed;
            Self.Animator.Play("Kick");
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev) && Self.IsOnFloor())
            {
                Self.ChangeState<PlayerWalkJumpState>();
                return;
            }

            if (InputService.RollJustPressed(ev))
            {
                if (Self.IsOnFloor())
                    Self.ChangeState<PlayerRollState>();
                else
                    Self.ChangeState<PlayerDiveState>();

                return;
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            ApplyHitboxToBreakableObjects(
                Self.KickHitbox,
                null,
                b => b.VulnerableToKick,
                b => b.OnKicked()
            );

            // It's possible for the objects hit by the hitbox to change the
            // current state(IE: because you bonked, or you freed a fairy).
            // If that happens, we don't want the normal logic
            // for this state to run for an extra frame.
            if (!IsCurrent)
                return;

            ApplyGravity(delta);
            StrafeWithLeftStick(Player.Walk.Speed, Player.Walk.Accel, delta);

            if (!Self.LocalVelocity.Flattened().IsZeroApprox())
                RotateInstantlyTowardVelocity();

            Self.MoveAndSlide();

            _timer -= delta;
            if (_timer <= 0)
            {
                if (Self.IsOnFloor())
                {
                    Self.ChangeState<PlayerWalkState>();
                }
                else
                {
                    Self.ChangeState<PlayerKickFlopState>();
                }
            }
        }
    }
}