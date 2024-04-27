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

        public void SetShowPerformanceStatus(bool showPerformanceStats)
        {
            _userSettings.ShowPerformanceStats = showPerformanceStats;
            _userSettings.SaveToJson();
        }
    }
}