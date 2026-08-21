using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class StatSystemTests
    {
        [Test]
        public void GetValue_ReturnsBase_WhenNoModifiers()
        {
            var stats = new StatSystem();
            stats.SetBase(StatType.MoveSpeed, 3f);

            Assert.AreEqual(3f, stats.GetValue(StatType.MoveSpeed));
        }

        [Test]
        public void GetValue_AppliesAdditiveModifier()
        {
            var stats = new StatSystem();
            stats.SetBase(StatType.MaxHealth, 100f);
            stats.AddModifier(new StatModifier { Stat = StatType.MaxHealth, Type = ModifierType.Additive, Value = 20f });

            Assert.AreEqual(120f, stats.GetValue(StatType.MaxHealth));
        }

        [Test]
        public void GetValue_AppliesMultiplicativeModifier()
        {
            var stats = new StatSystem();
            stats.SetBase(StatType.AttackDamage, 10f);
            stats.AddModifier(new StatModifier { Stat = StatType.AttackDamage, Type = ModifierType.Multiplicative, Value = 0.5f });

            Assert.AreEqual(15f, stats.GetValue(StatType.AttackDamage));
        }

        [Test]
        public void GetValue_ClampsToCap()
        {
            var stats = new StatSystem();
            stats.SetBase(StatType.Armor, 0.4f);
            stats.SetCap(StatType.Armor, 0.5f);
            stats.AddModifier(new StatModifier { Stat = StatType.Armor, Type = ModifierType.Additive, Value = 0.5f });

            Assert.AreEqual(0.5f, stats.GetValue(StatType.Armor));
        }

        [Test]
        public void RemoveModifier_StopsApplyingIt()
        {
            var stats = new StatSystem();
            stats.SetBase(StatType.MoveSpeed, 3f);
            var modifier = new StatModifier { Stat = StatType.MoveSpeed, Type = ModifierType.Additive, Value = 2f };
            stats.AddModifier(modifier);
            stats.RemoveModifier(modifier);

            Assert.AreEqual(3f, stats.GetValue(StatType.MoveSpeed));
        }
    }
}
