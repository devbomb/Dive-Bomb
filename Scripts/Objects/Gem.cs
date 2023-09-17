using Godot;

namespace FastDragon
{
    public partial class Gem : CharacterBody3D
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

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            if (!IsOnFloor())
                Velocity += Vector3.Down * 9.8f * delta;
            else
                Velocity = Vector3.Zero;

            MoveAndCollide(Velocity * delta);
        }

        public void OnCollectionAreaBodyEntered(Node3D body)
        {
            if (body is Player && CurrentState == State.Revealed)
                Collect();
        }

        public void Reveal()
        {
            CurrentState = State.Revealed;
            GD.Print($"Revealed gem {GetPath()}");
        }

        public void Collect()
        {
            SaveFile.Current.TotalGemCount += (int)Value;
            SaveFile.Current.CollectedGems.Add(GetPath());
            CurrentState = State.Collected;

            GD.Print($"{SaveFile.Current.TotalGemCount}: Collected gem {GetPath()}");
        }
    }
}