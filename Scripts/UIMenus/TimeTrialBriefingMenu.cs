using Godot;

namespace FastDragon
{
    public partial class TimeTrialBriefingMenu : Page
    {
        [Signal] public delegate void StartPressedEventHandler();

        public void OnStartPressed() => EmitSignal(SignalName.StartPressed);

        public override void _Process(double deltaD)
        {
            // HACK: Keep the game paused even after the fade-to-black finishes
            GetTree().Paused = true;
        }
    }
}