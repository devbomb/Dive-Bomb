using Godot;

namespace FastDragon
{
    public partial class PlayerWalkSlowPivotState : PlayerState
    {
        // TODO: move this to PlayerState
        protected float YawRad
        {
            get => _player.GlobalRotation.Y;
            set
            {
                var rot = _player.GlobalRotation;
                rot.Y = value;
                _player.GlobalRotation = rot;
            }
        }

        private float _timer;
        private float _startYawRad;
        private Vector3 _startVelocity;

        public override void OnStateEntered()
        {
            _timer = 0;
            _startYawRad = YawRad;
            _startVelocity = _player.Velocity;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;
            _timer += delta;

            float t = _timer / Player.Walk.SlowPivotTime;
            t = Mathf.Min(t, 1);

            // Accelerate toward the target velocity
            Vector3 targetVelocity = LeftStick3D() * Player.Walk.SlowPivotSpeed;
            _player.Velocity = _startVelocity.Lerp(targetVelocity, t);

            // Rotate in the direction the player is pointing
            Vector2 leftStick2D = InputService.LeftStick;

            if (!leftStick2D.IsZeroApprox())
            {
                float targetYawRad = Transform3D.Identity
                    .LookingAt(LeftStick3D(), Vector3.Up)
                    .Basis
                    .GetEuler()
                    .Y;

                YawRad = Mathf.Lerp(_startYawRad, targetYawRad, t);
            }

            // Move
            _player.MoveAndSlide();

            // Go to the walking state when time is up
            if (_timer >= Player.Walk.SlowPivotTime)
                _player.ChangeState<PlayerWalkState>();
        }
    }
}