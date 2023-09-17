using Godot;

namespace FastDragon
{
    public partial class Gem : Node3D
    {
        [Export] public GemColor Value;
        public bool Collected {get; private set;} = false;

        public override void _Process(double delta)
        {
            Visible = !Collected;
        }

        public void OnCollectionAreaBodyEntered(Node3D body)
        {
            if (body is Player && !Collected)
            {
                SaveFile.Current.TotalGemCount += (int)Value;
                SaveFile.Current.CollectedGems.Add(GetPath());
                Collected = true;

                GD.Print($"Collected gem {GetPath()}");
            }
        }
    }
}