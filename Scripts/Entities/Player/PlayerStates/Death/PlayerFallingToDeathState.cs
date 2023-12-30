using Godot;

namespace FastDragon
{
    public partial class PlayerFallingToDeathState : PlayerState
    {
        public override bool Invincible => true;

        public const float FallDuration = 1;

        private float _timer = 0;
        private Vector3 _initialCameraPos;

        public override void OnStateEntered()
        {
            _player.Animator.Play("Flop");
            _timer = FallDuration;
            _player.Camera.DisableInput = true;
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
            _player.Velocity += Vector3.Down * Player.Default.Gravity * delta;
            _player.MoveAndSlide();

            if (_timer <= 0)
                MapTransitionManager.Instance.RespawnPlayerAfterDeath();
        }
    }
}