using Godot;

namespace FastDragon
{
    public static class AnimationTreeExtensions
    {
        public static void PlayState(
            this AnimationTree animationTree,
            string stateName,
            bool travel = false)
        {
            var playback = (AnimationNodeStateMachinePlayback)animationTree.Get("parameters/playback");

            if (travel)
                playback.Travel(stateName);
            else
                playback.Start(stateName);
        }

        public static string CurrentState(this AnimationTree animationTree)
        {
            var playback = (AnimationNodeStateMachinePlayback)animationTree.Get("parameters/playback");
            return playback.GetCurrentNode();
        }
    }
}