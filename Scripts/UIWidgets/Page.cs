using Godot;

namespace FastDragon
{
    public partial class Page : Control
    {
        [Signal] public delegate void BackRequestedEventHandler();

        public virtual void OnPageEntered() {}

        public void GoBack()
        {
            EmitSignal(SignalName.BackRequested);
        }
    }
}