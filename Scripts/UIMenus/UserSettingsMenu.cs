using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class UserSettingsMenu : Control
    {
        private UserSettings _userSettings => UserSettings.Instance;

        public void SetShowPerformanceStatus(bool showPerformanceStats)
        {
            _userSettings.ShowPerformanceStats = showPerformanceStats;
            _userSettings.SaveToJson();
        }
    }
}