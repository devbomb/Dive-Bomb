using Godot;

namespace FastDragon
{
    public partial class PortalLoadingScreen : Node3D
    {
        private string _levelSceneFile;

        private Node3D _playerModel => GetNode<Node3D>("%PlayerModel");
        private AnimationPlayer _playerAnimator => GetNode<AnimationPlayer>("%PlayerAnimator");
        private OrbitCamera _camera => GetNode<OrbitCamera>("%OrbitCamera");

        private WorldEnvironment _worldEnv => GetNode<WorldEnvironment>("%WorldEnv");

        public void Initialize(
            string levelSceneFile,
            Environment skyBoxEnvironment,
            double animationStartTime,
            Vector3 playerRotRad,
            float cameraDist,
            float cameraYawRad,
            float cameraPitchRad
        )
        {
            _levelSceneFile = levelSceneFile;
            _worldEnv.Environment = skyBoxEnvironment;

            // Sync up the player's animation
            _playerAnimator.Play("PlayerAnimations/Glide");
            _playerAnimator.Seek(animationStartTime, true);

            _playerModel.Rotation = playerRotRad;

            _camera.ChangeState<OrbitCameraLockedState>();
            _camera.OrbitDistance = cameraDist;
            _camera.OrbitYawRad = cameraYawRad;
            _camera.OrbitPitchRad = cameraPitchRad;

            // TODO: Load the level asynchronously instead of faking it with a
            // timer
            GetTree().CreateTimer(2).Timeout += () =>
            {
                MapTransitionManager.Instance.GoToMap(_levelSceneFile);
            };
        }
    }
}