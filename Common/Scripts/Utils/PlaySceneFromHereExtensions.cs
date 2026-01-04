using Godot;

namespace FastDragon
{
    public static class PlaySceneFromHereExtensions
    {
        public static Transform3D CameraPos => ProjectSettings
            .GetSetting("temp/play_from_here/pos")
            .AsTransform3D();

        public static bool PlaySceneFromHereWasUsed(this Node node)
        {
            if (!ProjectSettings.HasSetting("temp/play_from_here/scene"))
                return false;

            string targetScene = ProjectSettings.GetSetting("temp/play_from_here/scene").AsString();
            return node.GetTree().CurrentScene.SceneFilePath == targetScene;
        }
    }
}