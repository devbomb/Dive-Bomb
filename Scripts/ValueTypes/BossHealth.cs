using System.Linq;

namespace FastDragon
{
    /// <summary>
    /// Lets you split a boss's health up into phases.
    ///
    /// Instead of specifying which health thresholds correspond to which phase,
    /// you specify how much health each phase has.
    ///
    /// This allows you to tweak an individual phase's length without affecting
    /// the length of other phases.
    /// </summary>
    public struct BossHealth
    {
        public readonly int[] PhaseMaxHealths = new int[0];
        public int CurrentPhase {get; private set;} = 0;
        public int CurrentPhaseDamage {get; private set;} = 0;

        public int MaxHealth => PhaseMaxHealths.Sum();
        public int CurrentHealth => MaxHealth - TotalDamageTaken();

        public BossHealth(int[] phaseMaxHealths)
        {
            PhaseMaxHealths = phaseMaxHealths;
        }

        /// <summary>
        /// Deals 1 damage.  Returns true if this triggered a new phase
        /// </summary>
        /// <returns></returns>
        public bool Damage()
        {
            CurrentPhaseDamage++;

            if (CurrentPhaseDamage >= PhaseMaxHealths[CurrentPhase])
            {
                CurrentPhase++;
                CurrentPhaseDamage = 0;
                return true;
            }

            return false;
        }

        public int TotalDamageTaken()
        {
            // Yes, this is accurate.
            // CurrentPhase starts at 0, so:
            // * There are no phases that come before phase 0.
            // * There is 1 phase that comes before phase 1 (phase 0)
            // * There are 2 phases that come before phase 2 (phase 0, phase 1)
            // All of this is a long-winded way that there is no off-by-one
            // error here.
            int numPreviousPhases = CurrentPhase;

            int totalHealthOfPreviousPhases = PhaseMaxHealths
                .Take(numPreviousPhases)
                .Sum();

            return CurrentPhaseDamage + totalHealthOfPreviousPhases;
        }
    }
}