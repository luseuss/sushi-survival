using NUnit.Framework;
using SushiSurvival.Enemies.Boss;
using UnityEngine;

namespace SushiSurvival.EditModeTests
{
    public class BossAimLogicTests
    {
        // ---- PredictTarget ----

        [Test]
        public void PredictTarget_ZeroLead_AimsAtCurrentPosition()
        {
            Vector2 target = BossAimLogic.PredictTarget(new Vector2(2f, 1f), new Vector2(3f, 0f), 0.7f, 0f);
            Assert.AreEqual(2f, target.x, 0.0001f);
            Assert.AreEqual(1f, target.y, 0.0001f);
        }

        [Test]
        public void PredictTarget_FullLead_LandsWhereAConstantRunnerWillBe()
        {
            Vector2 target = BossAimLogic.PredictTarget(Vector2.zero, new Vector2(3f, 0f), 0.5f, 1f);
            Assert.AreEqual(1.5f, target.x, 0.0001f);
            Assert.AreEqual(0f, target.y, 0.0001f);
        }

        [Test]
        public void PredictTarget_PartialLead_IsProportional()
        {
            Vector2 target = BossAimLogic.PredictTarget(Vector2.zero, new Vector2(4f, 0f), 1f, 0.5f);
            Assert.AreEqual(2f, target.x, 0.0001f);
        }

        [Test]
        public void PredictTarget_StandingStill_AimsAtCurrentPosition()
        {
            Vector2 target = BossAimLogic.PredictTarget(new Vector2(5f, 5f), Vector2.zero, 1f, 1f);
            Assert.AreEqual(5f, target.x, 0.0001f);
            Assert.AreEqual(5f, target.y, 0.0001f);
        }

        [Test]
        public void PredictTarget_OutOfRangeLeadAndNegativeTime_AreClamped()
        {
            Vector2 over = BossAimLogic.PredictTarget(Vector2.zero, new Vector2(2f, 0f), 1f, 9f);
            Assert.AreEqual(2f, over.x, 0.0001f);

            Vector2 negativeTime = BossAimLogic.PredictTarget(Vector2.zero, new Vector2(2f, 0f), -1f, 1f);
            Assert.AreEqual(0f, negativeTime.x, 0.0001f);
        }

        // ---- ChargeDirection ----

        [Test]
        public void ChargeDirection_PointsFromBossToPlayerAsUnitVector()
        {
            Vector2 dir = BossAimLogic.ChargeDirection(new Vector2(0f, 0f), new Vector2(0f, 5f), Vector2.right);
            Assert.AreEqual(0f, dir.x, 0.0001f);
            Assert.AreEqual(1f, dir.y, 0.0001f);
        }

        [Test]
        public void ChargeDirection_Overlapping_UsesFallback()
        {
            Vector2 dir = BossAimLogic.ChargeDirection(Vector2.one, Vector2.one, Vector2.down);
            Assert.AreEqual(0f, dir.x, 0.0001f);
            Assert.AreEqual(-1f, dir.y, 0.0001f);
        }

        [Test]
        public void ChargeDirection_OverlappingWithNoFallback_DefaultsToRight()
        {
            Vector2 dir = BossAimLogic.ChargeDirection(Vector2.one, Vector2.one, Vector2.zero);
            Assert.AreEqual(1f, dir.x, 0.0001f);
            Assert.AreEqual(0f, dir.y, 0.0001f);
        }
    }
}
