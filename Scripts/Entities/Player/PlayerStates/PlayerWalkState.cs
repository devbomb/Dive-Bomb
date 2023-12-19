using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerWalkState : PlayerState
    {
        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraFreeState>();
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

            float rotSpeedDeg = _player.Velocity.Length() < (Player.Walk.Speed * 0.75f)
                ? Player.Walk.FastPivotRotSpeedDeg
                : Player.Walk.RotSpeedDeg;
            RotateTowardLeftStick(Mathf.DegToRad(rotSpeedDeg), delta);
            StrafeWithLeftStick(Player.Walk.Speed, Player.Walk.Accel, delta);
            _player.MoveAndSlide();

            if (InputService.ChargeHeld)
            {
                _player.ChangeState<PlayerChargeState>();
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

