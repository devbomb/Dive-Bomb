using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerWalkAfterChargingState : PlayerState
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

            RotateTowardLeftStick(Mathf.DegToRad(Player.Walk.RotSpeedDeg), delta);
            AccelerateWithLeftStick(
                Player.Walk.Speed,
                Player.Walk.Accel,
                Player.Walk.Decel,
                delta
            );

            _player.MoveAndSlide();

            if (InputService.ChargeHeld)
            {
                _player.ChangeState<PlayerChargeState>();
                return;
            }

            // Initiate a slow pivot if the player tries to turn by too much.
            // Skilled players can avoid this by jumping and turning in mid-air.
            // Clunky?  Yes, but that's intentional.  If you wanna be agile, you
            // need to git gud.
            var leftStick3D = LeftStick3D();
            if (!leftStick3D.IsZeroApprox())
            {
                float angleRad = leftStick3D.AngleTo(_player.GlobalForward());

                if (angleRad > Mathf.DegToRad(Player.SlowPivot.MinAngleDeg))
                {
                    _player.ChangeState<PlayerWalkSlowPivotState>();
                    return;
                }
            }

            // Once the player has slowed back down to a normal speed, resume
            // the normal, more-preceise walking controls
            if (_player.Velocity.Length() <= Player.Walk.Speed)
            {
                _player.ChangeState<PlayerWalkState>();
                return;
            }

            if (!_player.IsOnFloor())
            {
                _player.ChangeState<PlayerFlopState>();
                return;
            }
        }
    }
}

