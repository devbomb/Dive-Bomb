using Godot;

namespace FastDragon
{
    public partial class PlayerFallingToDeathState : PlayerState
    {
        public override bool Invincible => true;
        public override bool DisableCameraInput => true;

        public const float FallDuration = 2;

        private float _timer = 0;
        private Vector3 _initialCameraPos;

        public override void OnStateEntered()
        {
            Self.Animator.Play("ParachuteOpen");
            Self.Animator.Queue("Parachute");

            _timer = FallDuration;
            _initialCameraPos = Self.Camera.GlobalPosition;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            _timer -= delta;

            // The parachute is out, so slow down the fall.
            // Can you imagine if we didn't?  "You had ONE job, parachute!"
            Self.Velocity = Self.Velocity.DecayToward(Vector3.Down * 5, 1, delta);
            Self.MoveAndSlide();

            // Aim the camera at the player without following
            var camera = Self.Camera;
            camera.GlobalPosition = _initialCameraPos;

            Vector3 cameraToPlayerDir = Self.GlobalPosition - camera.GlobalPosition;
            cameraToPlayerDir = cameraToPlayerDir.Normalized();
            Vector3 targetRot = cameraToPlayerDir.ForwardToEulerAnglesRad();

            camera.GlobalRotation = camera.GlobalRotation.DecayTowardsEulerRad(
                targetRot,
                50,
                delta
            );

            // Respawn when the timer is finally up
            if (_timer <= 0)
                MapTransitionManager.Instance.RespawnPlayerAfterDeath();
        }
    }
}