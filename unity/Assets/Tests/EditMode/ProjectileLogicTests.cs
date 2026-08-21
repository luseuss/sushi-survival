using NUnit.Framework;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class ProjectileLogicTests
    {
        [Test]
        public void ShouldDespawn_AfterFirstHit_WhenPierceIsZero()
        {
            // 기획서 Lv1: 관통 0 = 1체만 타격 후 소멸
            Assert.IsTrue(ProjectileLogic.ShouldDespawn(enemiesHit: 1, pierceCount: 0));
        }

        [Test]
        public void ShouldDespawn_False_BeforeAnyHit()
        {
            Assert.IsFalse(ProjectileLogic.ShouldDespawn(enemiesHit: 0, pierceCount: 0));
        }

        [Test]
        public void ShouldDespawn_False_OnFirstHit_WhenPierceIsOne()
        {
            Assert.IsFalse(ProjectileLogic.ShouldDespawn(enemiesHit: 1, pierceCount: 1));
        }

        [Test]
        public void ShouldDespawn_True_OnSecondHit_WhenPierceIsOne()
        {
            Assert.IsTrue(ProjectileLogic.ShouldDespawn(enemiesHit: 2, pierceCount: 1));
        }

        [Test]
        public void ShouldDespawn_True_OnThirdHit_WhenPierceIsTwo()
        {
            // Lv4: 관통 2 = 3체까지 타격
            Assert.IsTrue(ProjectileLogic.ShouldDespawn(enemiesHit: 3, pierceCount: 2));
        }
    }
}
