using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class UserSettingsMenu : Control
    {
        private UserSettings _userSettings => UserSettings.Instance;
        private Control _buttons => GetNode<Control>("%Buttons");

        public void OnOpened()
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