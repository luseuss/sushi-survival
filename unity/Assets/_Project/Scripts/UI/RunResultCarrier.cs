using System.Collections.Generic;

namespace SushiSurvival.Core
{
    public static class RunResultCarrier
    {
        public static RunOutcome Outcome;
        public static float ElapsedTime;
        public static int Level;
        public static int KillCount;
        public static IReadOnlyList<AugmentCount> Augments;
    }
}