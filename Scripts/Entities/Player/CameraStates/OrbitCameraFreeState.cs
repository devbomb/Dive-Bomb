using Godot;

namespace FastDragon
{
    public partial class OrbitCameraFreeState : OrbitCameraState
    {
        [Export] public float FollowDistance = 6;
        [Export] public float ZoomSpeed = 4;

        [Export] public bool AllowMouseLook = false;
        [Export] public float MouseSensitivity = 0.0001f;
        [Export] public float RightStickRotSpeedDeg = 180;

        [Export] public float MinOrbitPitchDeg = -89;
        [Export] public float MaxOrbitPitchDeg = 0;


        public override void _Input(InputEvent ev)
        {
            if (ev is InputEventMouseMotion mouse && AllowMouseLook)
            {
                _camera.OrbitYawRad += mouse.Velocity.X * MouseSensitivity;
                _camera.OrbitPitchRad += mouse.Velocity.Y * MouseSensitivity;
                ClampOrbitAngles();
            }
        }

        public override void _Process(double deltaD)
        {
            float delta = (float)deltaD;

            float rotSpeed = Mathf.DegToRad(RightStickRotSpeedDeg);
            _camera.OrbitYawRad += InputService.RightStick.X * rotSpeed * delta;
            _camera.OrbitPitchRad += InputService.RightStick.Y * rotSpeed * delta;
            ClampOrbitAngles();

            _camera.OrbitDistance = Mathf.MoveToward(
                _camera.OrbitDistance,
                FollowDistance,
                ZoomSpeed * delta
            );
        }

        private void ClampOrbitAngles()
        {
            _camera.OrbitYawRad = Mathf.PosMod(_camera.OrbitYawRad, Mathf.DegToRad(360));
            _camera.OrbitPitchRad = Mathf.Clamp(
                _camera.OrbitPitchRad,
                Mathf.DegToRad(MinOrbitPitchDeg),
                Mathf.DegToRad(MaxOrbitPitchDeg)
            );
        }
    }
}