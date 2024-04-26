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
            _player.Animator.Play("ParachuteOpen");
            _player.Animator.Queue("Parachute");

            _timer = FallDuration;
            _initialCameraPos = _player.Camera.GlobalPosition;
        }

        public override void _Process(double deltaD)
        {
            float delta = (float)deltaD;

            var camera = _player.Camera;
            camera.GlobalPosition = _initialCameraPos;

            Vector3 cameraToPlayerDir = _player.GlobalPosition - camera.GlobalPosition;
            cameraToPlayerDir = cameraToPlayerDir.Normalized();
            Vector3 targetRot = cameraToPlayerDir.ForwardToEulerAnglesRad();

            camera.GlobalRotation = camera.GlobalRotation.DecayTowardsEulerRad(
                targetRot,
                50,
                delta
            );
            camera.ResetPhysicsInterpolation();
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            _timer -= delta;

            // The parachute is out, so slow down the fall.
            // Can you imagine if we didn't?  "You had ONE job, parachute!"
            _player.Velocity = _player.Velocity.DecayToward(Vector3.Down * 5, 1, delta);

            _player.MoveAndSlide();

            if (_timer <= 0)
                MapTransitionManager.Instance.RespawnPlayerAfterDeath();
        }
    }
}