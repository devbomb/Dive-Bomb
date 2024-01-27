using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerWalkState : PlayerState
    {
        private const string WalkAnim = "Walk";
        private const string SkidAnim = "Skid";

        private const float StrideLength = 1;
        private const float BounceHeight = 0.1f;

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
            _player.Model.Position = Vector3.Zero;
        }

        public override void _Process(double deltaD)
        {
            float delta = (float)deltaD;

            AngleModelPitchWithGroundSlope(delta);

            // Adjust the animation speed to match our actual speed
            float animLen = (float)_player.Animator.CurrentAnimationLength;
            float distancePerCycle = StrideLength * 2;
            float speed = _player.Velocity.Length();
            float speedScale = speed * animLen / distancePerCycle;
            _player.Animator.SpeedScale = speedScale;

            // Add a little "bounce" to the step.
            // This isn't part of the animation because its height needs to vary
            // according to the speed
            float height = BounceHeight / speedScale;
            float interval = (float)_player.Animator.CurrentAnimationLength / 2;
            float t = (float)(_player.Animator.CurrentAnimationPosition / interval);
            _player.Model.Position = Vector3.Up * height * Parabola(t);
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                _player.ChangeState<PlayerWalkJumpState>();
                return;
            }

            if (InputService.RollJustPressed(ev))
            {
                _player.ChangeState<PlayerRollState>();
                return;
            }

            if (InputService.KickJustPressed(ev))
            {
                _player.ChangeState<PlayerKickState>();
                return;
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            StrafeWithLeftStick(Player.Walk.Speed, Player.Walk.Accel, delta);
            RotateInstantlyTowardVelocity();
            _player.MoveAndSlide();

            PlaySkidAnimIfTurningHard();

            if (InputService.RollHeld)
            {
                _player.ChangeState<PlayerRollState>();
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

        private float Parabola(float t)
        {
            t %= 1f;
            float x = (2 * t) - 1;
            return 1 - (x * x);
        }
    }
}

