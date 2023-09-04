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

            RotateTowardLeftStick(Mathf.DegToRad(Player.Walk.RotSpeedDeg), delta);
            AccelerateWithLeftStick(Player.Walk.Speed, Player.Walk.Accel, delta);

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
            if (leftStick3D.IsZeroApprox())
            {
                float angleRad = leftStick3D.AngleTo(_player.GlobalForward());

                if (angleRad > Mathf.DegToRad(Player.Walk.SlowPivotMinAngleDeg))
                {
                    _player.ChangeState<PlayerWalkSlowPivotState>();
                    return;
                }
            }

            if (!_player.IsOnFloor())
            {
                _player.ChangeState<PlayerWalkFallState>();
                return;
            }
        }

        private Vector3 LeftStick3D()
        {
            Vector2 leftStick2D = InputService.LeftStick;
            Vector3 cameraRot = _player.Camera.Rotation;

            Vector3 unrotated =
                (Vector3.Right * leftStick2D.X) +
                (Vector3.Forward * leftStick2D.Y);

            return unrotated.Rotated(Vector3.Up, cameraRot.Y);
        }
    }
}

