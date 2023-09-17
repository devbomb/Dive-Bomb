using Godot;

namespace FastDragon
{
    public partial class Gem : Node3D
    {
        [Export] public GemColor Value;
        public bool Collected {get; private set;} = false;

        private Vector3 _initialPos;

        public override void _Ready()
        {
            _initialPos = Position;

            SignalBus.Instance.LevelReset += () =>
            {
                Position = _initialPos;
                Collected = SaveFile.Current.CollectedGems.Contains(GetPath());
            };
        }

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