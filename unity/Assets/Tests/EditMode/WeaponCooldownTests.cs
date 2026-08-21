using NUnit.Framework;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class WeaponCooldownTests
    {
        [Test]
        public void IsReady_TrueImmediately_SoFirstAttackFiresAtOnce()
        {
            var cooldown = new WeaponCooldown();

            Assert.IsTrue(cooldown.IsReady);
        }

        [Test]
        public void IsReady_FalseRightAfterReset()
        {
            var cooldown = new WeaponCooldown();
            cooldown.Reset(1.2f);

            Assert.IsFalse(cooldown.IsReady);
        }

        [Test]
        public void IsReady_TrueAfterEnoughTimePasses()
        {
            var cooldown = new WeaponCooldown();
            cooldown.Reset(1.0f);
            cooldown.Tick(0.6f);
            cooldown.Tick(0.6f);

            Assert.IsTrue(cooldown.IsReady);
        }

        [Test]
        public void IsReady_FalseWhileTimeRemains()
        {
            var cooldown = new WeaponCooldown();
            cooldown.Reset(1.0f);
            cooldown.Tick(0.6f);

            Assert.IsFalse(cooldown.IsReady);
        }

        [Test]
        public void IsReady_TrueExactlyAtZero()
        {
            var cooldown = new WeaponCooldown();
            cooldown.Reset(1.0f);
            cooldown.Tick(1.0f);

            Assert.IsTrue(cooldown.IsReady);
        }
    }
}
