using Godot;

namespace FastDragon
{
    public partial class FollowCamera : Camera3D
    {
        [Export] public Node3D FollowTarget;
        [Export] public float FollowDistance = 6;
        [Export] public float FollowHeight = 3;

        public override void _Process(double deltaD)
        {
            // Move the camera toward the target on the x/z plane
            var flatTargetPos = FollowTarget.GlobalPosition.Flattened();
            var newPos = GlobalPosition.Flattened();
            float distance = newPos.DistanceTo(flatTargetPos);

            if (distance > FollowDistance)
            {
                distance -= FollowDistance;
                newPos += newPos.DirectionTo(flatTargetPos) * distance;
            }

            // Always keep it locked at the desired height
            newPos.Y = FollowTarget.GlobalPosition.Y + FollowHeight;

            // Apply the movement
            GlobalPosition = newPos;

            // Look at the target
            LookAt(FollowTarget.GlobalPosition);
        }
    }
}