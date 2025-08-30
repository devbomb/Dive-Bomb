using Godot;

namespace FastDragon
{
    public partial class TimeTrialBriefingMenu : Page
    {
        [Signal] public delegate void StartPressedEventHandler();

        public override void OnPageEntered()
        {
            var timeTrialManager = GetTree().FindNode<TimeTrialManager>();
            GetNode<Control>($"%Instructions/{timeTrialManager.Mode}").Visible = true;

            GetNode<Button>("%StartButton").GrabFocus();
        }

        public override void _Process(double deltaD)
        {
            // HACK: Keep the game paused even after the fade-to-black finishes
            GetTree().Paused = true;
        }

        public void OnStartPressed() => EmitSignal(SignalName.StartPressed);

        public void OnQuitToTitlePressed()
        {
            LevelTransitionManager.Instance.GoToTitleScreen();
        }
    }
}