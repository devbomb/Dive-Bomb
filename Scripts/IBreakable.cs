namespace FastDragon
{
    public interface IBreakable
    {
        /// <summary>
        /// Whether or not the player bonks when they roll or dive into this.
        /// Kicking never causes a bonk.
        /// </summary>
        bool CausesBonk => false;

        /// <summary>
        /// Whether or not <see cref="OnBroken"/> is called when rolled or dived
        /// into.  If true, then the player will also pass through this object
        /// intangibly while diving/rolling.
        /// </summary>
        bool VulnerableToRoll => true;

        /// <summary>
        /// Whether or not <see cref="OnBroken"/> is called when kicked.
        /// </summary>
        bool VulnerableToKick => true;

        /// <summary>
        /// Gets called whenever this object is rolled or dived into, regardless
        /// of whether or not it is vulnerable to rolling/diving.
        ///
        /// Use this to play "reaction" animations when the player tries to
        /// roll/dive through an unrollable/undivable object
        /// </summary>
        void OnRolledInto() {}

        /// <summary>
        /// Gets called whenever this object is kicked, regardless of whether or
        /// not it's vulnerable to kicking.
        ///
        /// Use this to play "reaction" animations when the player tries to
        /// kick an unkickable object.
        /// </summary>
        void OnKicked() {}

        /// <summary>
        /// Gets called whenever it is hit by an attack that it is vulnerable
        /// to.
        /// </summary>
        void OnBroken() {}
    }
}