using Godot;

namespace FastDragon
{
    public partial class Checkpoint : Area3D
    {
        [Export] public string CheckpointName;

        public override void _Ready()
        {
            if (CheckpointName == null)
                throw new System.Exception("CheckpointName cannot be null");
        }
    }
}