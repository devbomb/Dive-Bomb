#nullable enable
using Godot;

namespace FastDragon
{
    public class LoadingScreenParameters
    {
        /// <summary>
        ///     The level being loaded
        /// </summary>
        public required LevelManifest TargetLevel { get; init; }

        /// <summary>
        ///     The level we're exiting, if we're currently exiting a level.
        ///     Null if we're entering a level.
        /// </summary>
        public LevelManifest? PreviousLevel { get; init; }
        public required Environment SkyBoxEnvironment { get; init; }

        /// <summary>
        /// A clone of the main DirectionalLight3D from the previous level.
        /// Included so the loading screen can avoid jarring lighting changes.
        /// </summary>
        public required DirectionalLight3D OldSun { get; init; }

        public required string AnimationName { get; init; }
        public double AnimationStartTime { get; init; }
        public Vector3 PlayerStartRotRad { get; init; }

        public Vector3 CameraFocusPos { get; init; }
        public float CameraDist { get; init; }
        public float CameraYawRad { get; init; }
        public float CameraPitchRad { get; init; }

        public static LoadingScreenParameters FromCurrentLevel(
            LevelManifest targetLevel,
            LevelManifest? previousLevel,
            Environment skyBoxEnvironment,
            SceneTree sceneTree
        )
        {
            var oldScene = sceneTree.CurrentScene;
            var oldPlayer = oldScene.FindNode<Player>();

            return new LoadingScreenParameters
            {
                TargetLevel = targetLevel,
                PreviousLevel = previousLevel,
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