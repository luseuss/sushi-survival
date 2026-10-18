using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class WasabiRewardLogicTests
    {
        [Test]
        public void TransformableWeapon_GetsWeaponTransform()
        {
            Assert.AreEqual(WasabiReward.WeaponTransform, WasabiRewardLogic.Decide(true, false));
        }

        [Test]
        public void AlreadyTransformed_GetsNothing_AndHidesButton()
        {
            WasabiReward reward = WasabiRewardLogic.Decide(true, true);

            Assert.AreEqual(WasabiReward.None, reward);
            Assert.IsFalse(WasabiRewardLogic.ShowsButton(reward));
        }

        [Test]
        public void AlreadyTransformed_WinsOverCanTransform()
        {
            Assert.AreEqual(WasabiReward.None, WasabiRewardLogic.Decide(false, true));
        }

        [Test]
        public void NoTransformableWeapon_FallsBackToStatBuff_AndShowsButton()
        {
            WasabiReward reward = WasabiRewardLogic.Decide(false, false);

            Assert.AreEqual(WasabiReward.StatBuff, reward);
            Assert.IsTrue(WasabiRewardLogic.ShowsButton(reward));
        }

        [Test]
        public void BonusPercent_RoundsAndClamps()
        {
            Assert.AreEqual(25, WasabiRewardLogic.BonusPercent(1.25f));
            Assert.AreEqual(0, WasabiRewardLogic.BonusPercent(1f));
            Assert.AreEqual(0, WasabiRewardLogic.BonusPercent(0.5f));
        }
    }
}
