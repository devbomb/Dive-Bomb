using Godot;

namespace FastDragon
{
    public partial class PlayerChargeJumpState : PlayerState
    {
        public override bool AllowFlaming => false;
        public override bool DisableCameraInput => true;
        public override bool SpawningGemsHomeIn => true;

        public override void OnStateEntered()
        {
            _player.Animator.Play("ChargeJump");
            _player.VSpeed = Player.Charge.JumpVSpeed;
        }

        public override void OnStateExited()
        {
            ResetModelPitch();
        }

        public override void _Input(InputEvent ev)
        {
            GlideWithJumpButton(ev);
        }

        public override void _Process(double deltaD)
        {
            float delta = (float)deltaD;

            AngleModelPitchWithVelocity(delta);

            ContinuouslyRecenterCamera(
                Player.Charge.CameraDistance,
                Player.Charge.CameraPitchDeg,
                Player.Charge.CameraDecayRate,
                delta
            );
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            TurningControls(
                Player.Charge.AirSpeed,
                Player.Charge.TurnSpeedDeg,
                delta
            );
            ApplyGravity(delta);

            if (MoveAndSlideCharging(delta))
                return;

            if (_player.IsOnFloor())
            {
                _player.ChangeState<PlayerChargeState>();
                return;
            }
        }
    }
}