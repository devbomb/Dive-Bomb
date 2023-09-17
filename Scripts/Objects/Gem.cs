using Godot;

namespace FastDragon
{
    public partial class Gem : Node3D
    {
        [Export] public GemColor Value;

        public enum State
        {
            Hidden,
            Revealed,
            Collected
        }
        public State CurrentState = State.Revealed;

        private Vector3 _initialPos;
        private State _initialState;

        public override void _Ready()
        {
            _initialPos = Position;
            _initialState = CurrentState;

            SignalBus.Instance.LevelReset += Reset;
        }

        public void Reset()
        {
            Position = _initialPos;
            CurrentState = SaveFile.Current.CollectedGems.Contains(GetPath())
                ? State.Collected
                : _initialState;
        }

        public override void _Process(double delta)
        {
            Visible = CurrentState == State.Revealed;
        }

        public void OnCollectionAreaBodyEntered(Node3D body)
        {
            if (body is Player && CurrentState == State.Revealed)
            {
                SaveFile.Current.TotalGemCount += (int)Value;
                SaveFile.Current.CollectedGems.Add(GetPath());
                CurrentState = State.Collected;

                GD.Print($"Collected gem {GetPath()}");
            }
        }
    }
}