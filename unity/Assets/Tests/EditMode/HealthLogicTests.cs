using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class HealthLogicTests
    {
        [Test]
        public void ApplyDamage_ReducesHealth()
        {
            Assert.AreEqual(70f, HealthLogic.ApplyDamage(100f, 30f));
        }

        [Test]
        public void ApplyDamage_ClampsAtZero()
        {
            Assert.AreEqual(0f, HealthLogic.ApplyDamage(10f, 999f));
        }

        [Test]
        public void ApplyDamage_IgnoresNegativeDamage()
        {
            Assert.AreEqual(100f, HealthLogic.ApplyDamage(100f, -50f));
        }

        [Test]
        public void IsDead_TrueAtZero()
        {
            Assert.IsTrue(HealthLogic.IsDead(0f));
        }

        [Test]
        public void IsDead_FalseAboveZero()
        {
            Assert.IsFalse(HealthLogic.IsDead(1f));
        }
    }
}
