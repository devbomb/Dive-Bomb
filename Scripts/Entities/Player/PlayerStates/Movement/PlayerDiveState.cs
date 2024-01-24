using Godot;

namespace FastDragon
{
    public partial class PlayerDiveState : PlayerState
    {
        public override bool AllowFlaming => false;
        public override bool DisableCameraInput => true;
        public override bool SpawningGemsHomeIn => true;

        public override void OnStateEntered()
        {
            _player.Animator.Play("Dive");
            _player.VSpeed = Player.Dive.InitialVSpeed;
        }

        public override void OnStateExited()
        {
            ResetModelPitch();
        }

        public override void _Process(double deltaD)
        {
            float delta = (float)deltaD;

            AngleModelPitchWithVelocity(delta);

            ContinuouslyRecenterCamera(
                Player.Dive.CameraDistance,
                Player.Dive.CameraPitchDeg,
                Player.Dive.CameraDecayRate,
                delta
            );
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            TurningControls(
                Player.Dive.FSpeed,
                Player.Dive.TurnSpeedDeg,
                delta
            );
            ApplyGravity(delta, Player.Dive.Gravity);

            if (MoveAndSlideCharging(delta))
                return;

            if (_player.IsOnFloor())
            {
                _player.ChangeState<PlayerRollState>();
                return;
            }
        }
    }
}