using Godot;

namespace FastDragon
{
    public partial class SaveFileGDScriptAdaptor : Node
    {
        public SaveFile Current() => SaveFileManager.Current;
    }
}