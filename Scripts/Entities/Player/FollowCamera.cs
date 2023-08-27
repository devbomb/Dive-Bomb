using Godot;

namespace FastDragon
{
    public partial class FollowCamera : Camera3D
    {
        [Export] public Node3D FollowTarget;
        [Export] public float FollowDistance = 6;
        [Export] public float MouseSensitivity = 0.0001f;
        [Export] public float MinOrbitPitchDeg = -90;
        [Export] public float MaxOrbitPitchDeg = 0;

        private float _orbitYawRad;
        private float _orbitPitchRad;

        public override void _Input(InputEvent ev)
        {
            if (ev is InputEventMouseMotion mouse)
            {
                _orbitYawRad += mouse.Velocity.X * MouseSensitivity;
                _orbitPitchRad += mouse.Velocity.Y * MouseSensitivity;

                _orbitYawRad = Mathf.PosMod(_orbitYawRad, Mathf.DegToRad(360));
                _orbitPitchRad = Mathf.Clamp(
                    _orbitPitchRad,
                    Mathf.DegToRad(MinOrbitPitchDeg),
                    Mathf.DegToRad(MaxOrbitPitchDeg)
                );
            }
        }

        public override void _Process(double deltaD)
        {
            Vector3 dir = Vector3.Back
                .Rotated(Vector3.Right, _orbitPitchRad)
                .Rotated(Vector3.Up, _orbitYawRad);

            GlobalPosition = FollowTarget.GlobalPosition + (dir * FollowDistance);

            LookAt(FollowTarget.GlobalPosition);
        }
    }
}