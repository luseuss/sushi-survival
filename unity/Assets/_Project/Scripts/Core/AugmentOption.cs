using UnityEngine;
using SushiSurvival.Data;
using SushiSurvival.Player;

namespace SushiSurvival.Core
{
    public class AugmentOption : IUpgradeOption
    {
        private readonly PlayerStats _stats;
        private readonly PlayerHealth _health;

        public AugmentData Data { get; }

        public string DisplayName => Data.augmentName;
        public Sprite Icon => Data.icon;

        public AugmentOption(AugmentData data, PlayerStats stats, PlayerHealth health)
        {
            Data = data;
            _stats = stats;
            _health = health;
        }

        public void Apply()
        {
            _stats.AddModifier(new StatModifier
            {
                Stat = Data.statType,
                Type = ModifierType.Additive,
                Value = Data.valuePerPick
            });

            // 최대체력 증강은 현재 체력도 같이 올려야 체감이 된다.
            if (Data.statType == StatType.MaxHealth && _health != null)
                _health.GrantMaxHealthIncrease(Data.valuePerPick);
        }
    }
}
