using Godot;

namespace FastDragon
{
    public partial class TimeTrialBriefingMenu : Page
    {
        [Signal] public delegate void StartPressedEventHandler();
        [Signal] public delegate void ExitPressedEventHandler();

        private Control _instructionsRoot => GetNode<Control>("%Instructions");

        public override void OnPageEntered()
        {
            var mode = this.GetLevel().TimeTrial.Mode;
            ShowInstructionsFor(mode);

            GetNode<Button>("%StartButton").GrabFocus();
        }

        public override void _Process(double deltaD)
        {
            // HACK: Keep the game paused even after the fade-to-black finishes
            GetTree().Paused = true;
        }

        public void OnStartPressed() => EmitSignal(SignalName.StartPressed);
        public void OnExitPressed() => EmitSignal(SignalName.ExitPressed);

        private void ShowInstructionsFor(TimeTrialCategory? category)
        {
            foreach (var label in _instructionsRoot.GetChildren())
            {
                (label as Label).Visible = false;
            }

            if (category != null)
                GetNode<Control>($"%Instructions/{category}").Visible = true;
        }
    }
}