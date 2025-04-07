using Godot;

namespace FastDragon
{
    public partial class UserSettingsGDScriptAdaptor : Node
    {
        public UserSettings Instance() => UserSettings.Instance;
    }
}