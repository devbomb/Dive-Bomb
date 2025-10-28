using Godot;

namespace FastDragon
{
    public class LoadingScreenParameters
    {
        public string TargetLevelScenePath;
        public string PreviousLevelScenePath;
        public Environment SkyBoxEnvironment;

        /// <summary>
        /// A clone of the main DirectionalLight3D from the previous level.
        /// Included so the loading screen can avoid jarring lighting changes.
        /// </summary>
        public DirectionalLight3D OldSun;

        public string AnimationName;
        public double AnimationStartTime;
        public Vector3 PlayerStartRotRad;

        public Vector3 CameraFocusPos;
        public float CameraDist;
        public float CameraYawRad;
        public float CameraPitchRad;

        public static LoadingScreenParameters FromCurrentLevel(
            string targetLevelScenePath,
            string previousLevelScenePath,
            Environment skyBoxEnvironment,
            SceneTree sceneTree
        )
        {
            var oldScene = sceneTree.CurrentScene;
            var oldPlayer = oldScene.FindNode<Player>();

            return new LoadingScreenParameters
            {
                TargetLevelScenePath = targetLevelScenePath,
                PreviousLevelScenePath = previousLevelScenePath,
                SkyBoxEnvironment = skyBoxEnvironment,

                OldSun = (DirectionalLight3D)oldScene.FindNode<DirectionalLight3D>().Duplicate(),

                AnimationName = oldPlayer.Animator.AssignedAnimation,
                AnimationStartTime = oldPlayer.Animator.CurrentAnimationPosition,
                PlayerStartRotRad = oldPlayer.Model.GlobalRotation,

                CameraFocusPos = oldPlayer.CameraFocus.GlobalPosition - oldPlayer.GlobalPosition,
                CameraDist = oldPlayer.Camera.GlobalPosition.DistanceTo(oldPlayer.CameraFocus.GlobalPosition),
                CameraYawRad = oldPlayer.Camera.GlobalRotation.Y,
                CameraPitchRad = oldPlayer.Camera.GlobalRotation.X
            };
        }
    }
}