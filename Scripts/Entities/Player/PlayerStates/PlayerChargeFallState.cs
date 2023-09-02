using Godot;

namespace FastDragon
{
    public partial class PlayerChargeFallState : PlayerState
    {
        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraLockedState>();
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

            _player.MoveAndSlide();

            ContinuouslyRecenterCamera(
                Player.Charge.CameraDistance,
                Player.Charge.CameraPitchDeg,
                Player.Charge.CameraDecayRate,
                delta
            );

            if (_player.IsOnFloor())
            {
                _player.ChangeState<PlayerChargeState>();
                return;
            }
        }
    }
}