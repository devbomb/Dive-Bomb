using Godot;

namespace FastDragon
{
    public partial class PauseMenu : Control
    {
        private bool _open = false;

        public override void _Input(InputEvent ev)
        {
            if (InputService.PauseJustPressed(ev))
            {
                if (_open)
                    Close();
                else
                    Open();
            }
        }

        public override void _Ready()
        {
            Close();
        }

        public void Open()
        {
            _open = true;
            Visible = true;
            GetTree().Paused = true;
        }

        public void Close()
        {
            _open = false;
            Visible = false;
            GetTree().Paused = false;
        }

        public void ResetLevel()
        {
            Close();
            SignalBus.Instance.EmitLevelReset();
        }

        public void ExitLevel()
        {
            Close();
            MapTransitionManager.Instance.GoToLevelSelect();
        }
    }
}