using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Enemies.Boss
{
    /// <summary>
    /// 보스 체력이 소환 임계에 닿았는지 판정한다. 한 번의 큰 피해로 임계를 여러 개 넘을 수 있어서,
    /// "이번에 새로 넘은 단계 수"를 돌려주고 호출자가 그만큼 순서대로 소환한다(단계를 건너뛰지 않는다).
    /// </summary>
    public static class BossSummonLogic
    {
        /// <param name="nextStage">아직 소환하지 않은 첫 단계의 인덱스.</param>
        public static int CrossedStageCount(float currentHealth, float maxHealth,
                                            IReadOnlyList<BossSummonStage> stages, int nextStage)
        {
            if (maxHealth <= 0f || stages == null || nextStage < 0) return 0;

            float ratio = currentHealth / maxHealth;

            int count = 0;
            for (int k = nextStage; k < stages.Count; k++)
            {
                // "닿는 즉시"라서 이하로 비교한다.
                if (ratio > stages[k].healthThreshold) break;
                count++;
            }

            return count;
        }
    }
}
