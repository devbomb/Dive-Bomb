using Godot;

namespace FastDragon
{
    public partial class CopycatCamera : Camera3D
    {
        public override void _Ready()
        {
            ProcessPriority = int.MaxValue;
        }

        public override void _Process(double delta)
        {
            var mainCamera = GetTree().Root.GetCamera3D();
            Visible = mainCamera != null;

            if (Visible)
            {
                GlobalPosition = mainCamera.GlobalPosition;
                GlobalRotation = mainCamera.GlobalRotation;
            }
        }
    }
}