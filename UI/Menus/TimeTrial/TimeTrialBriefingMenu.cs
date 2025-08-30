using Godot;

namespace FastDragon
{
    public partial class TimeTrialBriefingMenu : Page
    {
        [Signal] public delegate void StartPressedEventHandler();

        public override void OnPageEntered()
        {
            var mode = this.GetLevel().TimeTrial.Mode;
            GetNode<Control>($"%Instructions/{mode}").Visible = true;

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