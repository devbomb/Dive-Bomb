using System;
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

        public static double PlayAnimStateGetLength(
            this AnimationTree animTree,
            string animName
        )
        {
            var stateMachine = (AnimationNodeStateMachine)animTree.TreeRoot;
            var node = stateMachine.GetNode(animName);
            if (node is not AnimationNodeAnimation animNode)
                throw new Exception($"There is no AnimationNodeAnimation named {animName}");

            animTree.PlayState(animName);
            return animNode.UseCustomTimeline
                ? animNode.TimelineLength
                : animTree.GetAnimation(animName).Length;
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