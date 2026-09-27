using Godot;

namespace FastDragon
{
    public static class AnimationTreeExtensions
    {
        public static void PlayState(
            this AnimationTree animTree,
            string stateName,
            bool travel = false)
        {
            var playback = animTree.GetPlayback();
            if (travel)
                playback.Travel(stateName);
            else
                playback.Start(stateName);
        }

        public static string CurrentState(this AnimationTree animTree)
        {
            return animTree.GetPlayback().GetCurrentNode();
        }

        public static AnimationPlayer GetAnimPlayer(this AnimationTree animationTree)
        {
            return animationTree.GetNode<AnimationPlayer>(animationTree.AnimPlayer);
        }

        private static AnimationNodeStateMachinePlayback GetPlayback(this AnimationTree animTree)
        {
            return (AnimationNodeStateMachinePlayback)animTree.Get("parameters/playback");
        }
    }
}