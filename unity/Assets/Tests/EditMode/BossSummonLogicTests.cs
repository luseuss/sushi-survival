using NUnit.Framework;
using SushiSurvival.Data;
using SushiSurvival.Enemies.Boss;

namespace SushiSurvival.EditModeTests
{
    public class BossSummonLogicTests
    {
        private static BossSummonStage[] Stages(params float[] thresholds)
        {
            var stages = new BossSummonStage[thresholds.Length];
            for (int i = 0; i < thresholds.Length; i++)
                stages[i] = new BossSummonStage { healthThreshold = thresholds[i], basicCount = 1 };
            return stages;
        }

        [Test]
        public void AboveFirstThreshold_ReturnsZero()
        {
            Assert.AreEqual(0, BossSummonLogic.CrossedStageCount(86f, 100f, Stages(0.85f, 0.7f, 0.55f), 0));
        }

        [Test]
        public void ExactlyAtThreshold_Counts()
        {
            Assert.AreEqual(1, BossSummonLogic.CrossedStageCount(85f, 100f, Stages(0.85f, 0.7f, 0.55f), 0));
        }

        [Test]
        public void BelowFirstThreshold_ReturnsOne()
        {
            Assert.AreEqual(1, BossSummonLogic.CrossedStageCount(84f, 100f, Stages(0.85f, 0.7f, 0.55f), 0));
        }

        [Test]
        public void BigHit_CrossesSeveralStagesAtOnce()
        {
            Assert.AreEqual(3, BossSummonLogic.CrossedStageCount(50f, 100f, Stages(0.85f, 0.7f, 0.55f), 0));
        }

        [Test]
        public void AlreadyCrossedStages_AreSkipped()
        {
            Assert.AreEqual(2, BossSummonLogic.CrossedStageCount(50f, 100f, Stages(0.85f, 0.7f, 0.55f), 1));
        }

        [Test]
        public void NextStagePastTheEnd_ReturnsZero()
        {
            Assert.AreEqual(0, BossSummonLogic.CrossedStageCount(1f, 100f, Stages(0.85f, 0.7f, 0.55f), 3));
        }

        [Test]
        public void ZeroOrNegativeMaxHealth_ReturnsZero()
        {
            Assert.AreEqual(0, BossSummonLogic.CrossedStageCount(0f, 0f, Stages(0.85f), 0));
            Assert.AreEqual(0, BossSummonLogic.CrossedStageCount(0f, -5f, Stages(0.85f), 0));
        }

        [Test]
        public void NullStages_ReturnsZero()
        {
            Assert.AreEqual(0, BossSummonLogic.CrossedStageCount(10f, 100f, null, 0));
        }

        [Test]
        public void NegativeNextStage_ReturnsZero()
        {
            Assert.AreEqual(0, BossSummonLogic.CrossedStageCount(10f, 100f, Stages(0.85f), -1));
        }

        [Test]
        public void Stage_Total_SumsAllMobKinds()
        {
            var stage = new BossSummonStage { basicCount = 3, californiaCount = 3, midCount = 2 };
            Assert.AreEqual(8, stage.Total);
        }
    }
}
