using NUnit.Framework;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class UpgradeDescriptionLogicTests
    {
        // ---- DescribeAugment ----

        [Test]
        public void DescribeAugment_MultiplierStats_ShowPercent()
        {
            Assert.AreEqual("공격력 +20%", UpgradeDescriptionLogic.DescribeAugment(StatType.AttackDamage, 0.2f));
            Assert.AreEqual("공격 속도 +10%", UpgradeDescriptionLogic.DescribeAugment(StatType.AttackSpeed, 0.1f));
            Assert.AreEqual("공격 범위 +10%", UpgradeDescriptionLogic.DescribeAugment(StatType.AttackRange, 0.1f));
            Assert.AreEqual("이동 속도 +12%", UpgradeDescriptionLogic.DescribeAugment(StatType.MoveSpeed, 0.12f));
            Assert.AreEqual("자석 범위 +15%", UpgradeDescriptionLogic.DescribeAugment(StatType.MagnetRange, 0.15f));
            Assert.AreEqual("경험치 획득량 +6%", UpgradeDescriptionLogic.DescribeAugment(StatType.ExpGain, 0.06f));
        }

        [Test]
        public void DescribeAugment_Armor_IsDamageReduction()
        {
            Assert.AreEqual("받는 피해 -5%", UpgradeDescriptionLogic.DescribeAugment(StatType.Armor, 0.05f));
        }

        [Test]
        public void DescribeAugment_FixedValueStats_ShowNumbers()
        {
            Assert.AreEqual("최대 체력 +30", UpgradeDescriptionLogic.DescribeAugment(StatType.MaxHealth, 30f));
            Assert.AreEqual("초당 체력 0.5 회복", UpgradeDescriptionLogic.DescribeAugment(StatType.Regen, 0.5f));
            Assert.AreEqual("쓰러져도 부활 +1회", UpgradeDescriptionLogic.DescribeAugment(StatType.Revive, 1f));
        }

        [Test]
        public void DescribeAugment_FloatNoiseDoesNotLeakIntoText()
        {
            // 0.1f * 100 은 10.000001이 될 수 있다.
            Assert.AreEqual("공격 속도 +10%", UpgradeDescriptionLogic.DescribeAugment(StatType.AttackSpeed, 0.1f));
            Assert.AreEqual("공격력 +30%", UpgradeDescriptionLogic.DescribeAugment(StatType.AttackDamage, 0.3f));
        }

        // ---- DescribeWeaponUpgrade ----

        private static WeaponLevelStats Stats(float damage, float cooldown, float range, float angle, int pierce)
            => new WeaponLevelStats { damage = damage, cooldown = cooldown, range = range, angleDegrees = angle, pierceCount = pierce };

        [Test]
        public void DescribeWeaponUpgrade_MeleeShowsDamageCooldownAndRange()
        {
            var text = UpgradeDescriptionLogic.DescribeWeaponUpgrade(Stats(8, 1.2f, 2.0f, 120, 0), Stats(10, 1.15f, 2.2f, 130, 0));

            Assert.AreEqual("공격력 8 → 10\n쿨타임 1.2 → 1.15초\n범위 2 → 2.2", text);
        }

        [Test]
        public void DescribeWeaponUpgrade_RangedShowsPierceInsteadOfRange()
        {
            var text = UpgradeDescriptionLogic.DescribeWeaponUpgrade(Stats(12, 0.8f, 0, 0, 0), Stats(14, 0.75f, 0, 0, 1));

            Assert.AreEqual("공격력 12 → 14\n쿨타임 0.8 → 0.75초\n관통 0 → 1", text);
        }

        [Test]
        public void DescribeWeaponUpgrade_UnchangedStatsAreOmitted()
        {
            var text = UpgradeDescriptionLogic.DescribeWeaponUpgrade(Stats(10, 1f, 2f, 120, 0), Stats(12, 1f, 2f, 120, 0));

            Assert.AreEqual("공격력 10 → 12", text);
        }

        [Test]
        public void DescribeWeaponUpgrade_NeverMoreThanThreeLines()
        {
            var text = UpgradeDescriptionLogic.DescribeWeaponUpgrade(Stats(5, 0.5f, 1.5f, 45, 0), Stats(6, 0.45f, 1.6f, 50, 1));

            Assert.AreEqual(3, text.Split('\n').Length);
            Assert.IsTrue(text.StartsWith("공격력"));
        }

        [Test]
        public void DescribeWeaponUpgrade_NothingChanged_IsEmpty()
        {
            var same = Stats(10, 1f, 2f, 120, 0);

            Assert.AreEqual(string.Empty, UpgradeDescriptionLogic.DescribeWeaponUpgrade(same, same));
        }
    }
}
