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

        [Export] public Slider CameraSensControllerSlider;
        private const float MinCameraSensController = 0.1f;
        private const float MaxCameraSensController = 10;

        [Export] public Slider CameraSensMouseSlider;
        private const float MinCameraSensMouse = 0.2f;
        private const float MaxCameraSensMouse = 5;

        private UserSettings _userSettings => UserSettings.Instance;
        private Control _buttons => GetNode<Control>("%Buttons");

        public override void OnPageEntered()
        {
            _buttons.GetChild<Button>(0).GrabFocus();

            MasterVolumeSlider.Value = _userSettings.MasterVolumeLinear;
            SfxVolumeSlider.Value = _userSettings.SfxVolumeLinear;
            MusicVolumeSlider.Value = _userSettings.MusicVolumeLinear;
            VoiceVolumeSlider.Value = _userSettings.DialogVoiceVolumeLinear;

            CameraSensControllerSlider.Value = ToScalingSlider(
                CameraSensControllerSlider,
                _userSettings.CameraSensController,
                MinCameraSensController,
                MaxCameraSensController
            );

            CameraSensMouseSlider.Value = ToScalingSlider(
                CameraSensMouseSlider,
                _userSettings.CameraSensMouse,
                MinCameraSensMouse,
                MaxCameraSensMouse
            );
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

        public void OnSliderChanged(float newValue)
        {
            _userSettings.MasterVolumeLinear = (float)MasterVolumeSlider.Value;
            _userSettings.SfxVolumeLinear = (float)SfxVolumeSlider.Value;
            _userSettings.MusicVolumeLinear = (float)MusicVolumeSlider.Value;
            _userSettings.DialogVoiceVolumeLinear = (float)VoiceVolumeSlider.Value;

            _userSettings.CameraSensController = FromScalingSlider(
                CameraSensControllerSlider,
                MinCameraSensController,
                MaxCameraSensController
            );

            _userSettings.CameraSensMouse = FromScalingSlider(
                CameraSensMouseSlider,
                MinCameraSensMouse,
                MaxCameraSensMouse
            );


            _userSettings.SaveToJson();
        }

        private static double ToScalingSlider(
            Slider slider,
            float value,
            float inputMin,
            float inputMax
        )
        {
            double sliderMid = (slider.MinValue + slider.MaxValue) / 2;

            if (value < 1)
            {
                double t = Mathf.InverseLerp(inputMin, 1, value);
                return Mathf.Lerp(slider.MinValue, sliderMid, t);
            }
            else
            {
                double t = Mathf.InverseLerp(1, inputMax, value);
                return Mathf.Lerp(sliderMid, slider.MaxValue, t);
            }
        }

        private static float FromScalingSlider(
            Slider slider,
            float outputMin,
            float outputMax
        )
        {
            double sliderMid = (slider.MinValue + slider.MaxValue) / 2;

            if (slider.Value < sliderMid)
            {
                float t = (float)Mathf.InverseLerp(slider.MinValue, sliderMid, slider.Value);
                return Mathf.Lerp(outputMin, 1, t);
            }
            else
            {
                float t = (float)Mathf.InverseLerp(sliderMid, slider.MaxValue, slider.Value);
                return Mathf.Lerp(1, outputMax, t);
            }
        }
    }
}