using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerWalkState : PlayerState
    {
        private const string WalkAnim = "Idle";
        private const string SkidAnim = "Skid";

        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraFreeState>();
            _player.Animator.Play(WalkAnim);

            if (_player.Velocity.Length() < Player.Walk.MinSpeed)
                _player.FSpeed = Player.Walk.MinSpeed;
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

            StrafeWithLeftStick(Player.Walk.Speed, Player.Walk.Accel, delta);
            RotateInstantlyTowardVelocity();
            _player.MoveAndSlide();

            PlaySkidAnimIfTurningHard();

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

            if (_player.Velocity.Length() < Player.Walk.MinSpeed)
            {
                _player.ChangeState<PlayerStandState>();
                return;
            }
        }

        private void PlaySkidAnimIfTurningHard()
        {
            float leftStickForwardComponent = LeftStick3D().ComponentAlong(_player.GlobalForward());
            bool playingSkid = _player.Animator.AssignedAnimation == SkidAnim;

            if (leftStickForwardComponent < 0 && !playingSkid)
            {
                _player.Animator.Play("Skid");
            }

            if (leftStickForwardComponent >= 0 && playingSkid)
            {
                _player.Animator.Play("Idle");
            }
        }
    }
}

