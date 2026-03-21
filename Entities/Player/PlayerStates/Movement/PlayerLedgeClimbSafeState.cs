using System;
using Godot;

namespace FastDragon
{
    public partial class PlayerLedgeClimbSafeState : PlayerState
    {
        private const double Duration = 0.4;

        private double _timer;
        private Vector3 _startPos;
        private Vector3 _targetPos;


        public override void OnStateEntered()
        {
            float rollAnimLen = Self.Animator.GetAnimation("Roll").Length;
            Self.Animator.Play(
                "Roll",
                customBlend: 0.125f,
                customSpeed: rollAnimLen / (float)Duration
            );

            _timer = 0;
            _startPos = Self.GlobalPosition;
            _targetPos = Self.LedgeDetector.DetectLedge().Value.LedgePoint;
        }

        public override void OnStateExited()
        {
            Self.Animator.Play("RESET", customBlend: 0);
            Self.Animator.Advance(0);
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.RollJustPressed(ev))
            {
                Self.ChangeState<PlayerDiveState>();
                return;
            }

            if (InputService.KickJustPressed(ev))
            {
                Self.ChangeState<PlayerKickState>();
                return;
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            _timer += delta;

            float t = (float)(_timer / Duration);
            t = Mathf.Min(t, 1);

            // Add an offset to the start and end to account for moving
            // platforms
            Vector3 offset = Self.LastPlatformVelocity * (float)_timer;
            Vector3 start = _startPos + offset;
            Vector3 end = _targetPos + offset;

            Self.GlobalPosition = start.LerpParabola(end, 1, t);

            if (_timer >= Duration)
            {
                Self.GlobalPosition = end;
                ChangeState<PlayerWalkState>();

                // Start the player off with the speed they _appeared_ to have
                // during the ledge climb animation.
                Self.FSpeed = (_targetPos - _startPos).Length() / (float)Duration;
                Self.VSpeed = -1;
            }
        }
    }
}