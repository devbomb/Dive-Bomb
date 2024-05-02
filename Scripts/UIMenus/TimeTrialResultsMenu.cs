using Godot;

namespace FastDragon
{
    public partial class TimeTrialResultsMenu : Page
    {
        public override void OnPageEntered()
        {
            GetNode<Button>("%RetryButton").GrabFocus();
        }

        public void OnRetryPressed() => MapTransitionManager.Instance.RespawnPlayerAfterDeath();
        public void OnQuitToTitlePressed() => MapTransitionManager.Instance.GoToTitleScreen();
    }
}