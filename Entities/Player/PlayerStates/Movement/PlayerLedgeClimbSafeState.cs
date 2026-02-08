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
            _targetPos = Self.LedgeDetector.LastLedgePoint;
        }

        public override void OnStateExited()
        {
            Self.Animator.Play("RESET", customBlend: 0);
            Self.Animator.Advance(0);
        }

        public override void _PhysicsProcess(double delta)
        {
            _timer += delta;

            float t = (float)(_timer / Duration);
            t = Mathf.Min(t, 1);

            Self.GlobalPosition = _startPos.LerpParabola(_targetPos, 1, t);

            if (_timer >= Duration)
            {
                Self.GlobalPosition = _targetPos;
                ChangeState<PlayerWalkState>();

                // Start the player off with the speed they _appeared_ to have
                // during the ledge climb animation.
                Self.FSpeed = (_targetPos - _startPos).Length() / (float)Duration;
                Self.VSpeed = -1;
            }
        }
    }
}