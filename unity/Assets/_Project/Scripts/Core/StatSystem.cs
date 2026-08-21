using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.Core
{
    public enum StatType
    {
        AttackDamage,
        AttackSpeed,
        AttackRange,
        MaxHealth,
        Armor,
        Regen,
        MoveSpeed,
        MagnetRange,
        ExpGain,
        Revive
    }

    public enum ModifierType
    {
        Additive,
        Multiplicative
    }

    public struct StatModifier
    {
        public StatType Stat;
        public ModifierType Type;
        public float Value;
    }

    /// <summary>
    /// base값 + modifier 목록으로 최종 스탯을 계산하고, 설정된 상한(cap) 안에서
    /// 클램프한다. 증강 10종과 호감도 대화 버프가 이 클래스를 공유해서 스탯을
    /// 한 곳에서만 클램프한다.
    /// </summary>
    public class StatSystem
    {
        private readonly Dictionary<StatType, float> _baseValues = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> _caps = new Dictionary<StatType, float>();
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();

        public void SetBase(StatType stat, float value) => _baseValues[stat] = value;

        public void SetCap(StatType stat, float capValue) => _caps[stat] = capValue;

        public void AddModifier(StatModifier modifier) => _modifiers.Add(modifier);

        public void RemoveModifier(StatModifier modifier) => _modifiers.Remove(modifier);

        public void ClearModifiers() => _modifiers.Clear();

        public float GetValue(StatType stat)
        {
            float baseValue = _baseValues.TryGetValue(stat, out var b) ? b : 0f;
            float additiveSum = 0f;
            float multiplicativeSum = 1f;

            foreach (var modifier in _modifiers)
            {
                if (modifier.Stat != stat) continue;
                if (modifier.Type == ModifierType.Additive) additiveSum += modifier.Value;
                else multiplicativeSum += modifier.Value;
            }

            float result = (baseValue + additiveSum) * multiplicativeSum;

            if (_caps.TryGetValue(stat, out var cap))
                result = Mathf.Min(result, cap);

            return result;
        }
    }
}
