using Godot;

namespace FastDragon
{
    public static class AnimationPlayerExtensions
    {
        /// <summary>
        /// Starts the given animation and returns its length in seconds.
        ///
        /// If a <paramref name="customSpeed"/> is supplied, then the returned
        /// length will be adjusted to compensate.
        /// </summary>
        /// <remarks>
        /// Use this instead of polling <see cref="AnimationPlayer.IsPlaying"/>
        /// when you have a <see cref="StateMachine"/> that needs to change
        /// states when then animation finishes.  That will ensure the state
        /// will always last a deterministic number of physics ticks, regardless
        /// of framerate fluctuations.
        ///
        /// Polling <see cref="AnimationPlayer.IsPlaying"/> is nondeterministic
        /// because animations advance on <see cref="Node._Process"/> instead of
        /// <see cref="Node._PhysicsProcess"/> by default.  Even if your polling
        /// takes place in <see cref="Node._PhysicsProcess"/>, there is still
        /// a chance for it to complete 1 tick sooner or later than expected if
        /// the framerate gets extremely low.
        /// </remarks>
        public static double PlayGetLength(
            this AnimationPlayer animator,
            StringName name,
            double customBlend = -1,
            float customSpeed = 1,
            bool fromEnd = false
        )
        {
            double length = animator.GetAnimation(name).Length;
            animator.Play(name, customBlend, customSpeed, fromEnd);
            return length / customSpeed;
        }

        /// <summary>
        /// Starts the given animation section and returns the section's length
        /// in seconds.
        ///
        /// If a <paramref name="customSpeed"/> is supplied, then the returned
        /// length will be adjusted to compensate.
        /// </summary>
        /// <remarks>
        /// Use this instead of polling <see cref="AnimationPlayer.IsPlaying"/>
        /// when you have a <see cref="StateMachine"/> that needs to change
        /// states when then animation finishes.  That will ensure the state
        /// will always last a deterministic number of physics ticks, regardless
        /// of framerate fluctuations.
        ///
        /// Polling <see cref="AnimationPlayer.IsPlaying"/> is nondeterministic
        /// because animations advance on <see cref="Node._Process"/> instead of
        /// <see cref="Node._PhysicsProcess"/> by default.  Even if your polling
        /// takes place in <see cref="Node._PhysicsProcess"/>, there is still
        /// a chance for it to complete 1 tick sooner or later than expected if
        /// the framerate gets extremely low.
        /// </remarks>
        public static double PlaySectionGetLength(
            this AnimationPlayer animator,
            StringName name = null,
            double startTime = -1,
            double endTime = -1,
            double customBlend = -1,
            float customSpeed = 1,
            bool fromEnd = false)
        {
            animator.PlaySection(
                name,
                startTime,
                endTime,
                customBlend,
                customSpeed,
                fromEnd
            );

            double start = startTime < 0
                ? 0
                : startTime;

            double end = endTime < 0
                ? animator.GetAnimation(name).Length
                : endTime;

            return end - start;
        }
    }
}