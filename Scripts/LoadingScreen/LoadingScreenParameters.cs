using Godot;

namespace FastDragon
{
    public class LoadingScreenParameters
    {
        public string TargetMapSceneFilePath;
        public string PreviousMapSceneFilePath;
        public Environment SkyBoxEnvironment;

        /// <summary>
        /// A clone of the main DirectionalLight3D from the previous map.
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

        public static LoadingScreenParameters FromCurrentMap(
            string targetMapSceneFilePath,
            string previousMapSceneFilePath,
            Environment skyBoxEnvironment,
            SceneTree sceneTree
        )
        {
            var oldScene = sceneTree.CurrentScene;
            var oldPlayer = oldScene.FindNode<Player>();

            return new LoadingScreenParameters
            {
                TargetMapSceneFilePath = targetMapSceneFilePath,
                PreviousMapSceneFilePath = previousMapSceneFilePath,
                SkyBoxEnvironment = skyBoxEnvironment,

                OldSun = (DirectionalLight3D)oldScene.FindNode<DirectionalLight3D>().Duplicate(),

                AnimationName = oldPlayer.Animator.AssignedAnimation,
                AnimationStartTime = oldPlayer.Animator.CurrentAnimationPosition,
                PlayerStartRotRad = oldPlayer.GlobalRotation,

                CameraFocusPos = oldPlayer.CameraFocus.GlobalPosition - oldPlayer.GlobalPosition,
                CameraDist = oldPlayer.Camera.OrbitDistance,
                CameraYawRad = oldPlayer.Camera.OrbitYawRad,
                CameraPitchRad = oldPlayer.Camera.OrbitPitchRad
            };
        }
    }
}