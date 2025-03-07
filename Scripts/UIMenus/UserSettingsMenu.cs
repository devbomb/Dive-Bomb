using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class UserSettingsMenu : Page
    {
        private UserSettings _userSettings => UserSettings.Instance;
        private Control _buttons => GetNode<Control>("%Buttons");

        public override void _Ready()
        {
            GetTree().PhysicsInterpolation = _userSettings.UsePhysicsInterpolation;
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
            GetTree().PhysicsInterpolation = toggledOn;
            _userSettings.SaveToJson();
        }
    }
}