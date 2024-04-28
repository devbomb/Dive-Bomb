using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class UserSettingsMenu : Page
    {
        private UserSettings _userSettings => UserSettings.Instance;
        private Control _buttons => GetNode<Control>("%Buttons");

        public override void _Input(InputEvent ev)
        {
            if (ev.IsActionPressed("ui_cancel"))
                GoBack();
        }

        public override void OnPageEntered()
        {
            _buttons.GetChild<Button>(0).GrabFocus();
        }

        public void ToggleShowPerformanceStatus(bool toggledOn)
        {
            _userSettings.ShowPerformanceStats = toggledOn;
            _userSettings.SaveToJson();
        }

        public void ToggleUsePhysicsInterpolation(bool toggledOn)
        {
            _userSettings.UsePhysicsInterpolation = toggledOn;
            _userSettings.SaveToJson();
        }
    }
}