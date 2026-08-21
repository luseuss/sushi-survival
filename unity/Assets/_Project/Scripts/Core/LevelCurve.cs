namespace SushiSurvival.Core
{
    public struct LevelProgress
    {
        public float XpTowardNext;
        public int LevelsGained;
    }

    public static class LevelCurve
    {
        /// <summary>다음 레벨까지 필요한 경험치. 레벨이 오를수록 선형으로 늘어난다.</summary>
        public static float GetRequiredXp(int level, float baseXp, float increment)
            => baseXp + increment * (level - 1);

        /// <summary>
        /// 누적된 경험치로 몇 레벨이 오르는지 계산한다. 황금 젬 하나로 2~3레벨이
        /// 한 번에 오를 수 있으므로 반복 처리한다.
        /// </summary>
        public static LevelProgress Resolve(float xpTowardNext, int currentLevel, float baseXp, float increment)
        {
            int gained = 0;
            int level = currentLevel;
            float remaining = xpTowardNext;

            while (true)
            {
                float required = GetRequiredXp(level, baseXp, increment);
                if (required <= 0f || remaining < required) break;

                remaining -= required;
                level++;
                gained++;
            }

            return new LevelProgress { XpTowardNext = remaining, LevelsGained = gained };
        }
    }
}
