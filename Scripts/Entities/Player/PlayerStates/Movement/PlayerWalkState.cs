using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerWalkState : PlayerState
    {
        private const string WalkAnim = "Walk";
        private const string SkidAnim = "Skid";

        private const float StrideLength = 1;

        public override void OnStateEntered()
        {
            _player.Animator.Play(WalkAnim);

            if (_player.Velocity.Length() < Player.Walk.MinSpeed)
                _player.FSpeed = Player.Walk.MinSpeed;
        }

        public override void OnStateExited()
        {
            ResetModelPitch();
            _player.Animator.SpeedScale = 1;
        }

        public override void _Process(double deltaD)
        {
            float delta = (float)deltaD;

            AngleModelPitchWithGroundSlope(delta);

            // Adjust the animation speed to match our actual speed
            // Adjust the animation speed to match our actual speed
            float animLen = (float)_player.Animator.CurrentAnimationLength;
            float distancePerCycle = StrideLength * 2;
            float speed = _player.Velocity.Length();

            _player.Animator.SpeedScale = speed * animLen / distancePerCycle;
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
                _player.Animator.Play(SkidAnim);
            }

            if (leftStickForwardComponent >= 0 && playingSkid)
            {
                _player.Animator.Play(WalkAnim);
            }
        }
    }
}

