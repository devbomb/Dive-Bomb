using Godot;

namespace FastDragon
{
    public partial class Checkpoint : Area3D
    {
        [Export] public string CheckpointName;

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;

            if (CheckpointName == null)
                throw new System.Exception("CheckpointName cannot be null");
        }

        private void OnBodyEntered(Node3D body)
        {
            GD.Print($"Body entered {body}");
            if (body is Player)
            {
                SaveFile.Current.CurrentCheckpoint = CheckpointName;
                GD.Print($"Set checkpoint to {CheckpointName}");
            }
        }
    }
}