using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class ArmorLogicTests
    {
        [Test]
        public void ApplyArmor_ReturnsFullDamage_WhenNoArmor()
        {
            Assert.That(ArmorLogic.ApplyArmor(100f, 0f), Is.EqualTo(100f).Within(0.0001f));
        }

        [Test]
        public void ApplyArmor_HalvesDamage_AtMaxArmor()
        {
            Assert.That(ArmorLogic.ApplyArmor(100f, 0.5f), Is.EqualTo(50f).Within(0.0001f));
        }

        [Test]
        public void ApplyArmor_ClampsToMaxArmor_NeverGrantsInvincibility()
        {
            // 방어력이 1.0으로 잘못 들어와도 절대 0 데미지가 되면 안 된다.
            Assert.That(ArmorLogic.ApplyArmor(100f, 1f), Is.EqualTo(50f).Within(0.0001f));
        }

        [Test]
        public void ApplyArmor_IgnoresNegativeArmor()
        {
            Assert.That(ArmorLogic.ApplyArmor(100f, -2f), Is.EqualTo(100f).Within(0.0001f));
        }

        [Test]
        public void ApplyArmor_ScalesPartialArmor()
        {
            Assert.That(ArmorLogic.ApplyArmor(100f, 0.2f), Is.EqualTo(80f).Within(0.0001f));
        }
    }
}
