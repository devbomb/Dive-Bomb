using Godot;

namespace FastDragon
{
    public partial class CameraSuggestionZone : Area3D
    {
        [Export] public float SuggestedYawDeg = 0;
        [Export] public float SuggestedPitchDeg = 0;
        [Export] public float SuggestedDistance = 6;

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
            BodyExited += OnBodyExited;
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
                player.Camera.StopSuggestingAngle();
            }
        }
    }
}