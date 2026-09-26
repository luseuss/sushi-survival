using System.Collections.Generic;
using NUnit.Framework;
using SushiSurvival.Enemies.Boss;

namespace SushiSurvival.EditModeTests
{
    public class BossPatternSchedulerTests
    {
        private static readonly BossPatternType[] All =
        {
            BossPatternType.Meteor, BossPatternType.Charge
        };

        [Test]
        public void AfterMaxConsecutive_NeverRepeatsThePreviousPattern()
        {
            foreach (var previous in All)
            {
                for (int phase = 1; phase <= 2; phase++)
                {
                    for (float roll = 0f; roll < 1f; roll += 0.05f)
                        Assert.AreNotEqual(previous,
                            BossPatternScheduler.SelectNext(previous, BossPatternScheduler.MaxConsecutive, phase, roll));
                }
            }
        }

        [Test]
        public void BelowMaxConsecutive_CanRepeatThePreviousPattern()
        {
            Assert.AreEqual(BossPatternType.Meteor,
                BossPatternScheduler.SelectNext(BossPatternType.Meteor, 1, 1, 0f));
            Assert.AreEqual(BossPatternType.Charge,
                BossPatternScheduler.SelectNext(BossPatternType.Charge, 1, 2, 0.99f));
        }

        [Test]
        public void PhaseOne_SplitsMeteorAndCharge()
        {
            // 1페이즈 가중치는 메테오 5 : 돌진 2라서 roll 5/7(약 0.714) 미만이 메테오다.
            Assert.AreEqual(BossPatternType.Meteor,
                BossPatternScheduler.SelectNext(BossPatternType.Charge, 1, 1, 0.0f));
            Assert.AreEqual(BossPatternType.Meteor,
                BossPatternScheduler.SelectNext(BossPatternType.Charge, 1, 1, 0.7f));
            Assert.AreEqual(BossPatternType.Charge,
                BossPatternScheduler.SelectNext(BossPatternType.Charge, 1, 1, 0.75f));
            Assert.AreEqual(BossPatternType.Charge,
                BossPatternScheduler.SelectNext(BossPatternType.Charge, 1, 1, 0.999f));
        }

        [Test]
        public void PhaseTwo_FavorsCharge()
        {
            // 2페이즈 가중치는 메테오 3 : 돌진 5라서 돌진이 더 넓은 구간을 차지한다.
            int charge = 0;
            int meteor = 0;
            for (int i = 0; i < 100; i++)
            {
                var next = BossPatternScheduler.SelectNext(BossPatternType.Meteor, 1, 2, i / 100f);
                if (next == BossPatternType.Charge) charge++;
                if (next == BossPatternType.Meteor) meteor++;
            }

            Assert.AreEqual(100, charge + meteor);
            Assert.Greater(charge, meteor);
        }

        [Test]
        public void OutOfRangeRoll_StillReturnsAValidPattern()
        {
            var max = BossPatternScheduler.MaxConsecutive;
            Assert.AreNotEqual(BossPatternType.Meteor,
                BossPatternScheduler.SelectNext(BossPatternType.Meteor, max, 1, -1f));
            Assert.AreNotEqual(BossPatternType.Meteor,
                BossPatternScheduler.SelectNext(BossPatternType.Meteor, max, 1, 5f));
        }

        [Test]
        public void EveryPatternIsReachable()
        {
            foreach (int phase in new[] { 1, 2 })
            {
                var seen = new HashSet<BossPatternType>();
                foreach (var previous in All)
                {
                    for (float roll = 0f; roll < 1f; roll += 0.01f)
                        seen.Add(BossPatternScheduler.SelectNext(previous, 1, phase, roll));
                }

                Assert.AreEqual(2, seen.Count);
            }
        }
    }
}
