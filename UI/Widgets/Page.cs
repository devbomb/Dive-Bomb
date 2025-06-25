using Godot;

namespace FastDragon
{
    public partial class Page : Control
    {
        [Signal] public delegate void BackRequestedEventHandler();

        /// <summary>
        /// The control that will receive focus when this page is navigated to
        /// </summary>
        [Export] public Control FocusedControl;

        public virtual void OnPageEntered() {}

        public void GoBack()
        {
            EmitSignal(SignalName.BackRequested);
        }
    }
}