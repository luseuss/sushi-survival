using NUnit.Framework;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class UmbrellaSkillLogicTests
    {
        private const float Eps = 0.0001f;

        [Test]
        public void IsReady_OnlyWhenCooldownIsOver()
        {
            Assert.IsTrue(UmbrellaSkillLogic.IsReady(0f));
            Assert.IsTrue(UmbrellaSkillLogic.IsReady(-0.1f));
            Assert.IsFalse(UmbrellaSkillLogic.IsReady(0.01f));
        }

        [Test]
        public void ShouldBuffer_OnlyJustBeforeReady()
        {
            Assert.IsTrue(UmbrellaSkillLogic.ShouldBuffer(0.1f, 0.25f));
            Assert.IsTrue(UmbrellaSkillLogic.ShouldBuffer(0.25f, 0.25f));
            Assert.IsFalse(UmbrellaSkillLogic.ShouldBuffer(0.3f, 0.25f), "쿨타임이 한참 남았으면 기억하지 않는다");
            Assert.IsFalse(UmbrellaSkillLogic.ShouldBuffer(0f, 0.25f), "이미 준비됐으면 기억할 필요가 없다");
        }

        [Test]
        public void ClickAccepted_IgnoresPausedTimeAndUiClicks()
        {
            Assert.IsTrue(UmbrellaSkillLogic.ClickAccepted(1f, false));
            Assert.IsFalse(UmbrellaSkillLogic.ClickAccepted(0f, false), "레벨업·일시정지 창이 떠 있을 때");
            Assert.IsFalse(UmbrellaSkillLogic.ClickAccepted(1f, true), "화면 위 버튼을 누르는 중");
        }

        [Test]
        public void FillRatio_GrowsAsCooldownDrains()
        {
            Assert.AreEqual(0f, UmbrellaSkillLogic.FillRatio(2f, 2f), Eps);
            Assert.AreEqual(0.5f, UmbrellaSkillLogic.FillRatio(1f, 2f), Eps);
            Assert.AreEqual(1f, UmbrellaSkillLogic.FillRatio(0f, 2f), Eps);
        }

        [Test]
        public void FillRatio_ClampsAndTreatsZeroDurationAsFull()
        {
            Assert.AreEqual(0f, UmbrellaSkillLogic.FillRatio(5f, 2f), Eps);
            Assert.AreEqual(1f, UmbrellaSkillLogic.FillRatio(-1f, 2f), Eps);
            Assert.AreEqual(1f, UmbrellaSkillLogic.FillRatio(0f, 0f), Eps);
        }

        [Test]
        public void PopScale_StartsAtZero_OvershootsThenSettlesAtOne()
        {
            Assert.AreEqual(0f, UmbrellaSkillLogic.PopScale(0f), Eps);
            Assert.AreEqual(1f, UmbrellaSkillLogic.PopScale(1f), Eps);

            float peak = 0f;
            for (int i = 0; i <= 20; i++)
                peak = System.Math.Max(peak, UmbrellaSkillLogic.PopScale(i / 20f));
            Assert.Greater(peak, 1f, "살짝 커졌다가 자리 잡는다");
            Assert.Less(peak, 1.3f);
        }

        [Test]
        public void PopScale_ClampsOutOfRange()
        {
            Assert.AreEqual(0f, UmbrellaSkillLogic.PopScale(-1f), Eps);
            Assert.AreEqual(1f, UmbrellaSkillLogic.PopScale(3f), Eps);
        }
    }
}
