using System.Collections.Generic;
using NUnit.Framework;
using SushiSurvival.Enemies.Boss;

namespace SushiSurvival.EditModeTests
{
    public class BossPatternSchedulerTests
    {
        private static readonly BossPatternType[] All =
        {
            BossPatternType.Meteor, BossPatternType.Summon, BossPatternType.Charge
        };

        [Test]
        public void SelectNext_NeverRepeatsThePreviousPattern()
        {
            foreach (var previous in All)
            {
                for (int phase = 1; phase <= 2; phase++)
                {
                    for (float roll = 0f; roll < 1f; roll += 0.05f)
                        Assert.AreNotEqual(previous, BossPatternScheduler.SelectNext(previous, phase, roll));
                }
            }
        }

        [Test]
        public void SelectNext_FromSummon_PhaseOne_SplitsMeteorAndCharge()
        {
            // 소환 직후 후보는 메테오(5)와 돌진(2). 5:2 가중치라 roll 5/7 미만이 메테오다.
            Assert.AreEqual(BossPatternType.Meteor,
                BossPatternScheduler.SelectNext(BossPatternType.Summon, 1, 0.0f));
            Assert.AreEqual(BossPatternType.Meteor,
                BossPatternScheduler.SelectNext(BossPatternType.Summon, 1, 0.7f));
            Assert.AreEqual(BossPatternType.Charge,
                BossPatternScheduler.SelectNext(BossPatternType.Summon, 1, 0.75f));
            Assert.AreEqual(BossPatternType.Charge,
                BossPatternScheduler.SelectNext(BossPatternType.Summon, 1, 0.999f));
        }

        [Test]
        public void SelectNext_PhaseTwo_FavorsCharge()
        {
            // 메테오 직후 후보는 소환(2)과 돌진(5). 돌진이 더 넓은 구간을 차지한다.
            int charge = 0;
            int summon = 0;
            for (int i = 0; i < 100; i++)
            {
                var next = BossPatternScheduler.SelectNext(BossPatternType.Meteor, 2, i / 100f);
                if (next == BossPatternType.Charge) charge++;
                if (next == BossPatternType.Summon) summon++;
            }

            Assert.AreEqual(100, charge + summon);
            Assert.Greater(charge, summon);
        }

        [Test]
        public void SelectNext_OutOfRangeRoll_StillReturnsAValidPattern()
        {
            Assert.AreNotEqual(BossPatternType.Meteor,
                BossPatternScheduler.SelectNext(BossPatternType.Meteor, 1, -1f));
            Assert.AreNotEqual(BossPatternType.Meteor,
                BossPatternScheduler.SelectNext(BossPatternType.Meteor, 1, 5f));
        }

        [Test]
        public void SelectNext_EveryPatternIsReachable()
        {
            foreach (int phase in new[] { 1, 2 })
            {
                var seen = new HashSet<BossPatternType>();
                foreach (var previous in All)
                {
                    for (float roll = 0f; roll < 1f; roll += 0.01f)
                        seen.Add(BossPatternScheduler.SelectNext(previous, phase, roll));
                }

                Assert.AreEqual(3, seen.Count);
            }
        }
    }
}
