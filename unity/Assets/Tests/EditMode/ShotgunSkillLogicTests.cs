using NUnit.Framework;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class ShotgunSkillLogicTests
    {
        private const float Eps = 0.0001f;

        [Test]
        public void ActiveRatio_DrainsFromFullToEmpty()
        {
            Assert.AreEqual(1f, ShotgunSkillLogic.ActiveRatio(5f, 5f), Eps);
            Assert.AreEqual(0.4f, ShotgunSkillLogic.ActiveRatio(2f, 5f), Eps);
            Assert.AreEqual(0f, ShotgunSkillLogic.ActiveRatio(0f, 5f), Eps);
        }

        [Test]
        public void ActiveRatio_ClampsAndTreatsZeroDurationAsEmpty()
        {
            Assert.AreEqual(1f, ShotgunSkillLogic.ActiveRatio(9f, 5f), Eps);
            Assert.AreEqual(0f, ShotgunSkillLogic.ActiveRatio(-1f, 5f), Eps);
            Assert.AreEqual(0f, ShotgunSkillLogic.ActiveRatio(1f, 0f), Eps);
        }

        [Test]
        public void ShouldStart_NeedsSkillReadyAndAClick()
        {
            Assert.IsTrue(ShotgunSkillLogic.ShouldStart(true, false, 0f, true));
            Assert.IsFalse(ShotgunSkillLogic.ShouldStart(false, false, 0f, true), "스킬을 아직 못 얻었다");
            Assert.IsFalse(ShotgunSkillLogic.ShouldStart(true, true, 0f, true), "이미 켜져 있다");
            Assert.IsFalse(ShotgunSkillLogic.ShouldStart(true, false, 1.5f, true), "쿨타임이 남았다");
            Assert.IsFalse(ShotgunSkillLogic.ShouldStart(true, false, 0f, false), "클릭이 없었다");
        }
    }
}
