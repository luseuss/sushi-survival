using SushiSurvival.Data;
using SushiSurvival.Player;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 대화에서 고른 증강을 스탯에 적용한다. AugmentOption.Apply()와 같은
    /// 패턴(모디파이어 추가 + 최대체력이면 현재체력도 같이 올림)이지만,
    /// AugmentOption은 Data.valuePerPick을 읽는 구조라 다른 값(비율×maxCap)을
    /// 넣으려면 억지로 끼워 맞춰야 해서 별도로 둔다.
    /// </summary>
    public static class AffinityBuffApplier
    {
        public static void Apply(AugmentData augment, float amount, PlayerStats stats, PlayerHealth health)
        {
            stats.AddModifier(new StatModifier
            {
                Stat = augment.statType,
                Type = ModifierType.Additive,
                Value = amount
            });

            // 최대체력 버프는 현재 체력도 같이 올려야 체감이 된다.
            if (augment.statType == StatType.MaxHealth && health != null)
                health.GrantMaxHealthIncrease(amount);
        }
    }
}
