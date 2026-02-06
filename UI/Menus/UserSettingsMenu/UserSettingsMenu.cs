using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class UserSettingsMenu : Page
    {
        [Export] public Slider MasterVolumeSlider;
        [Export] public Slider SfxVolumeSlider;
        [Export] public Slider MusicVolumeSlider;
        [Export] public Slider VoiceVolumeSlider;

        private UserSettings _userSettings => UserSettings.Instance;
        private Control _buttons => GetNode<Control>("%Buttons");

        public override void OnPageEntered()
        {
            _buttons.GetChild<Button>(0).GrabFocus();

            MasterVolumeSlider.Value = _userSettings.MasterVolumeLinear;
            SfxVolumeSlider.Value = _userSettings.SfxVolumeLinear;
            MusicVolumeSlider.Value = _userSettings.MusicVolumeLinear;
            VoiceVolumeSlider.Value = _userSettings.DialogVoiceVolumeLinear;
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

        public void ToggleInvertCameraX(bool toggledOn)
        {
            _userSettings.InvertCameraX = toggledOn;
            _userSettings.SaveToJson();
        }

        public void ToggleInvertCameraY(bool toggledOn)
        {
            _userSettings.InvertCameraY = toggledOn;
            _userSettings.SaveToJson();
        }

        public void OnVolumeSliderChanged(float newValue)
        {
            _userSettings.MasterVolumeLinear = (float)MasterVolumeSlider.Value;
            _userSettings.SfxVolumeLinear = (float)SfxVolumeSlider.Value;
            _userSettings.MusicVolumeLinear = (float)MusicVolumeSlider.Value;
            _userSettings.DialogVoiceVolumeLinear = (float)VoiceVolumeSlider.Value;
            _userSettings.SaveToJson();
        }
    }
}