using Godot;

namespace FastDragon
{
    public partial class MouseCaptureManager : Node
    {
        public static readonly StringName GroupName = "UncapturingMouse";

        public MouseCaptureManager()
        {
            ProcessMode = ProcessModeEnum.Always;
        }

        public override void _Process(double delta)
        {
            bool isCapturing = !GetTree().HasGroup(GroupName);
            Input.MouseMode = isCapturing
                ? Input.MouseModeEnum.Captured
                : Input.MouseModeEnum.Visible;
        }
    }

    public static class MouseCaptureNodeExtensions
    {
        public static void UncaptureMouse(this Node node)
        {
            node.AddToGroup(MouseCaptureManager.GroupName);
        }

        public static void RestoreMouseCapture(this Node node)
        {
            node.RemoveFromGroup(MouseCaptureManager.GroupName);
        }
    }
}