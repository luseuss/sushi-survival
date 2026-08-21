using NUnit.Framework;
using SushiSurvival.Enemies.Boss;

namespace SushiSurvival.EditModeTests
{
    public class BossPatternSchedulerTests
    {
        [Test]
        public void SelectNext_AfterMeteor_ReturnsSummon()
        {
            Assert.AreEqual(BossPatternType.Summon,
                BossPatternScheduler.SelectNext(BossPatternType.Meteor));
        }

        [Test]
        public void SelectNext_AfterSummon_ReturnsMeteor()
        {
            Assert.AreEqual(BossPatternType.Meteor,
                BossPatternScheduler.SelectNext(BossPatternType.Summon));
        }

        [Test]
        public void SelectNext_AlternatesOverManyCalls()
        {
            // 무작위로 뽑으면 소환이 연달아 나와 화면이 잡몹으로 덮인다.
            var current = BossPatternType.Summon;

            for (int i = 0; i < 10; i++)
            {
                var next = BossPatternScheduler.SelectNext(current);
                Assert.AreNotEqual(current, next);
                current = next;
            }
        }

        [Test]
        public void SelectNext_FromSummonSeed_StartsWithMeteor()
        {
            // BossController는 직전 패턴 초기값을 Summon으로 두어 첫 패턴이
            // 메테오가 되게 한다 — 등장하자마자 잡몹을 뿌리면 등장 연출이 묻힌다.
            Assert.AreEqual(BossPatternType.Meteor,
                BossPatternScheduler.SelectNext(BossPatternType.Summon));
        }
    }
}
