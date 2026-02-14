using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class CameraSuggestionZone : Area3D
    {
        [Export] public float SuggestedYawDeg = 0;
        [Export] public float SuggestedPitchDeg = 0;
        [Export] public float SuggestedDistance = 6;

        [Export] public string target;

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
            BodyExited += OnBodyExited;

            Callable.From(() =>
            {
                if (string.IsNullOrEmpty(target))
                    return;

                var marker = this.FindNodeByTargetName<NamedMarker3D>(target);
                SuggestedYawDeg = marker.GlobalRotationDegrees.Y;
                SuggestedPitchDeg = marker.GlobalRotationDegrees.X;
            }).CallDeferred();
        }

        public void OnBodyEntered(Node3D body)
        {
            if (body is Player player)
            {
                player.Camera.SuggestAngle(
                    Mathf.DegToRad(SuggestedYawDeg),
                    Mathf.DegToRad(SuggestedPitchDeg),
                    SuggestedDistance
                );
            }
        }

        public void OnBodyExited(Node3D body)
        {
            if (body is Player player)
            {
                player.Camera.StartFollowing();
            }
        }
    }
}