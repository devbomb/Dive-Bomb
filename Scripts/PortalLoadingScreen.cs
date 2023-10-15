using Godot;

namespace FastDragon
{
    public partial class PortalLoadingScreen : Node3D
    {
        private string _levelSceneFile;

        private Player _player => GetNode<Player>("%Player");
        private WorldEnvironment _worldEnv => GetNode<WorldEnvironment>("%WorldEnv");

        public void Initialize(
            string levelSceneFile,
            Environment skyBoxEnvironment
        )
        {
            _levelSceneFile = levelSceneFile;
            _worldEnv.Environment = skyBoxEnvironment;

            // TODO: Sync up the player's animation
            // TODO: Load the level asynchronously instead of faking it with a
            // timer
            GetTree().CreateTimer(2).Timeout += () =>
            {
                MapTransitionManager.Instance.GoToMap(_levelSceneFile);
            };
        }
    }
}