using Godot;

namespace FastDragon
{
    public partial class PlayerLedgeClimbState : PlayerState
    {
        private Vector3 _startPos;
        private Vector3 _endPos;
        private float _timer;

        private const float Duration = Player.LedgeGrab.ClimbDuration;

        public override void OnStateEntered()
        {
            float climbAnimLen = _player.Animator.GetAnimation("Roll").Length;
            _player.Animator.Play(
                "Roll",
                customBlend: Duration / 4,
                customSpeed: climbAnimLen / Duration
            );

            _startPos = _player.GlobalPosition;
            _endPos = _player.LedgeGrabPoint.GlobalPosition;
            _timer = 0;

            // Temporarily detatch the model so we can tween it separately.
            // That way, the model can go up and down, while the camera just
            // moves in a straight line.
            _player.Model.TopLevel = true;
            _player.Model.GlobalPosition = _player.GlobalPosition;
            _player.Model.GlobalRotation = _player.GlobalRotation;
        }

        public override void OnStateExited()
        {
            _player.GlobalPosition = _endPos;
            _player.Model.TopLevel = false;
            _player.Model.Position = Vector3.Zero;
            _player.Model.Rotation = Vector3.Zero;
            _player.Model.ResetPhysicsInterpolation();

            _player.Animator.Play("RESET", customBlend: 0);
            _player.Animator.Advance(0);
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            _timer += delta;
            _player.GlobalPosition = _startPos.Lerp(_endPos, _timer / Duration);

            _player.Model.GlobalPosition = LerpPartialParabola(
                _startPos,
                _endPos,
                _timer / Duration
            );

            if (_timer >= Duration)
                _player.ChangeState<PlayerWalkState>();
        }

        private Vector3 LerpPartialParabola(
            Vector3 start,
            Vector3 end,
            float t
        )
        {
            float height = end.Y - start.Y;

            Vector3 result = start.Flattened().Lerp(end.Flattened(), t);
            result.Y = _startPos.Y + PartialParabola(height, t);
            return result;
        }

        private float PartialParabola(float height, float t)
        {
            const float u = 1.5f;
            float L = Mathf.Sqrt(u / 2);
            float s = (Mathf.Sqrt(2 * u) + Mathf.Sqrt((2 * u) - 2)) / 2;
            float thingToBeSquared = (s * t) - L;
            float f = 2 - (2 * thingToBeSquared * thingToBeSquared);
            return height * f;
        }
    }
}