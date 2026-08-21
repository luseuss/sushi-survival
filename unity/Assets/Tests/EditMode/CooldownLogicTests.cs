using NUnit.Framework;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class CooldownLogicTests
    {
        [Test]
        public void ApplyAttackSpeed_ReturnsBase_WhenMultiplierIsOne()
        {
            Assert.That(CooldownLogic.ApplyAttackSpeed(1.2f, 1f, 0.1f), Is.EqualTo(1.2f).Within(0.0001f));
        }

        [Test]
        public void ApplyAttackSpeed_HalvesCooldown_AtDoubleSpeed()
        {
            Assert.That(CooldownLogic.ApplyAttackSpeed(1.2f, 2f, 0.1f), Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void ApplyAttackSpeed_RespectsMinimumCooldown()
        {
            // 이나리 기본 0.35초에 2배속이면 0.175초지만, 최소 쿨타임이 하한을 잡는다.
            Assert.That(CooldownLogic.ApplyAttackSpeed(0.35f, 2f, 0.25f), Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void ApplyAttackSpeed_FallsBackToBase_WhenMultiplierIsZero()
        {
            // 0으로 나누어 무한대가 되는 것을 막는다.
            Assert.That(CooldownLogic.ApplyAttackSpeed(1.2f, 0f, 0.1f), Is.EqualTo(1.2f).Within(0.0001f));
        }

        [Test]
        public void ApplyAttackSpeed_FallsBackToBase_WhenMultiplierIsNegative()
        {
            Assert.That(CooldownLogic.ApplyAttackSpeed(1.2f, -3f, 0.1f), Is.EqualTo(1.2f).Within(0.0001f));
        }
    }
}
