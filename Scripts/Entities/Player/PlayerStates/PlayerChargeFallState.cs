using Godot;

namespace FastDragon
{
    public partial class PlayerChargeFallState : PlayerState
    {
        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraLockedState>();
            _player.Animator.Play("ChargeJump");

            // Rob the player of any upward momentum they may have had.
            // This way, the player can charge while jumping to cut their jump
            // short.
            if (_player.Velocity.Y > 0)
                _player.VSpeed = 0;
        }

        public override void OnStateExited()
        {
            ResetModelPitch();
        }

        public override void _Process(double deltaD)
        {
            AngleModelPitchWithVelocity();

            ContinuouslyRecenterCamera(
                Player.Charge.CameraDistance,
                Player.Charge.CameraPitchDeg,
                Player.Charge.CameraDecayRate,
                (float)deltaD
            );
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            TurningControls(
                Player.Charge.AirSpeed,
                Player.Charge.AirTurnSpeedDeg,
                delta
            );
            ApplyGravity(delta);

            MoveAndSlideStepByStep(delta, OnChargedIntoSomething);

            if (IsTouchingWallAtBonkAngle())
            {
                _player.ChangeState<PlayerBonkState>();
                return;
            }

            if (_player.IsOnFloor())
            {
                _player.ChangeState<PlayerChargeState>();
                return;
            }
        }
    }
}