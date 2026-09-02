using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class LevelCurveTests
    {
        [Test]
        public void GetRequiredXp_ReturnsBase_AtLevelOne()
        {
            Assert.That(LevelCurve.GetRequiredXp(1, 5f, 3f), Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void GetRequiredXp_GrowsByIncrementPerLevel()
        {
            Assert.That(LevelCurve.GetRequiredXp(3, 5f, 3f), Is.EqualTo(11f).Within(0.0001f));
        }

        [Test]
        public void Resolve_GainsNothing_BelowThreshold()
        {
            var result = LevelCurve.Resolve(3f, 1, 5f, 3f);

            Assert.AreEqual(0, result.LevelsGained);
            Assert.That(result.XpTowardNext, Is.EqualTo(3f).Within(0.0001f));
        }

        [Test]
        public void Resolve_GainsOneLevel_ExactlyAtThreshold()
        {
            var result = LevelCurve.Resolve(5f, 1, 5f, 3f);

            Assert.AreEqual(1, result.LevelsGained);
            Assert.That(result.XpTowardNext, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void Resolve_CarriesRemainder()
        {
            var result = LevelCurve.Resolve(7f, 1, 5f, 3f);

            Assert.AreEqual(1, result.LevelsGained);
            Assert.That(result.XpTowardNext, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void Resolve_GainsMultipleLevels_FromOneBigGem()
        {
            // Lv1 필요 5, Lv2 필요 8 → 합계 13. 15를 넣으면 2레벨 오르고 2 남는다.
            var result = LevelCurve.Resolve(15f, 1, 5f, 3f);

            Assert.AreEqual(2, result.LevelsGained);
            Assert.That(result.XpTowardNext, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void GetProgressRatio_ReturnsHalf_WhenHalfwayToNextLevel()
        {
            // baseXp=5, increment=3, level=1 → 필요 경험치 5. 2.5는 절반.
            Assert.That(LevelCurve.GetProgressRatio(2.5f, 1, 5f, 3f), Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void GetProgressRatio_ReturnsZero_AtRunStart()
        {
            Assert.That(LevelCurve.GetProgressRatio(0f, 1, 5f, 3f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void GetProgressRatio_ClampsToOne_WhenOverfull()
        {
            // AddExperience가 넘친 경험치를 즉시 레벨업으로 소비하므로 실제로는
            // 잘 안 생기지만, 방어적으로 클램프한다.
            Assert.That(LevelCurve.GetProgressRatio(999f, 1, 5f, 3f), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void GetProgressRatio_ReturnsZero_WhenRequiredXpIsZero()
        {
            // 0으로 나누어 NaN이 되는 것을 막는다.
            Assert.That(LevelCurve.GetProgressRatio(1f, 1, 0f, 0f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void GetProgressRatio_ScalesWithLevel()
        {
            // baseXp=5, increment=3, level=3 → 필요 경험치 5+3*2=11. 절반은 5.5.
            Assert.That(LevelCurve.GetProgressRatio(5.5f, 3, 5f, 3f), Is.EqualTo(0.5f).Within(0.0001f));
        }
    }
}
