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
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            // Don't move, but do allow the player to spin in place
            _player.Velocity = _player.Velocity.MoveToward(Vector3.Zero, Player.Walk.Decel * delta);
            RotateTowardLeftStick(Mathf.DegToRad(Player.Stand.RotSpeedDeg), delta);
            _player.MoveAndSlide();

            if (InputService.ChargeHeld)
            {
                _player.ChangeState<PlayerRollState>();
                return;
            }

            if (!_player.IsOnFloor())
            {
                _player.ChangeState<PlayerFlopState>();
                return;
            }

            // If the player is facing the direction they're trying to walk
            // and is still pushing the left stick, then start walking.
            bool isPushingStick = !LeftStick3D().IsZeroApprox();
            float angleToStickRad = _player.GlobalForward().Flattened().AngleTo(LeftStick3D());
            if (isPushingStick && Mathf.IsZeroApprox(angleToStickRad))
            {
                _player.ChangeState<PlayerWalkState>();
                return;
            }
        }
    }
}

