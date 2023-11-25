using Godot;

namespace FastDragon
{
    public partial class ReturnHomeVortex : Node3D
    {
        private const float AscendSpeed = 4;
        private const float RotSpeedDeg = 180;
        private const float CameraRotSpeedDeg = 90;

        [Export] public float ExitHeight = 15;

        private Player _ensnaredPlayer = null;

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            if (_ensnaredPlayer == null)
                return;

            // Spin the player and move them up
            _ensnaredPlayer.GlobalPosition += Vector3.Up * AscendSpeed * delta;
            _ensnaredPlayer.GlobalRotationDegrees += Vector3.Up * RotSpeedDeg * delta;

            // Rotate the camera underneath the player
            _ensnaredPlayer.Camera.OrbitPitchRad = AngleMath.DecayToward(
                _ensnaredPlayer.Camera.OrbitPitchRad,
                Mathf.DegToRad(89.999f),
                2,
                delta
            );

            if (_ensnaredPlayer.GlobalPosition.Y >= GlobalPosition.Y + ExitHeight)
            {
                MapTransitionManager.Instance.ExitLevel();
            }
        }

        public void OnTriggerBodyEntered(Node body)
        {
            if (body is Player p && !(p.CurrentState is PlayerManhandledState))
            {
                _ensnaredPlayer = p;
                p.ChangeState<PlayerManhandledState>();
                p.Animator.Play("Glide");
            }
        }
    }
}