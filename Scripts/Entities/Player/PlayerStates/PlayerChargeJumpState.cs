using Godot;

namespace FastDragon
{
    public partial class PlayerChargeJumpState : PlayerState
    {
        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraLockedState>();
            _player.Animator.Play("ChargeJump");
            _player.VSpeed = Player.Charge.JumpVSpeed;
        }

        public override void OnStateExited()
        {
            ResetModelPitch();
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                _player.ChangeState<PlayerGlideState>();
            }
        }

        public override void _Process(double deltaD)
        {
            AngleModelPitchWithVelocity();
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            TurningControls(
                Player.Charge.AirSpeed,
                Player.Charge.AirTurnSpeedDeg,
                delta
            );
            ApplyGravity(delta, Player.Default.JumpRiseGravity);

            MoveAndSlideStepByStep(delta, OnChargedIntoSomething);

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