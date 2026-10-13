using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class GuideTabLogicTests
    {
        [Test]
        public void Gameplay_ShowsGameplayRoot_AndNoList()
        {
            Assert.IsTrue(GuideTabLogic.ShowsGameplayRoot(GuideTab.Gameplay));
            Assert.IsFalse(GuideTabLogic.ShowsList(GuideTab.Gameplay));
        }

        [Test]
        public void Augments_ShowsListWithAugmentRowsOnly()
        {
            Assert.IsFalse(GuideTabLogic.ShowsGameplayRoot(GuideTab.Augments));
            Assert.IsTrue(GuideTabLogic.ShowsList(GuideTab.Augments));
            Assert.IsTrue(GuideTabLogic.ShowsAugmentRows(GuideTab.Augments));
            Assert.IsFalse(GuideTabLogic.ShowsFairyRows(GuideTab.Augments));
        }

        [Test]
        public void Fairy_ShowsListWithFairyRowsOnly()
        {
            Assert.IsFalse(GuideTabLogic.ShowsGameplayRoot(GuideTab.Fairy));
            Assert.IsTrue(GuideTabLogic.ShowsList(GuideTab.Fairy));
            Assert.IsFalse(GuideTabLogic.ShowsAugmentRows(GuideTab.Fairy));
            Assert.IsTrue(GuideTabLogic.ShowsFairyRows(GuideTab.Fairy));
        }

        [Test]
        public void ExactlyOneContentAreaIsVisibleForEveryTab()
        {
            foreach (GuideTab tab in System.Enum.GetValues(typeof(GuideTab)))
            {
                int visible = (GuideTabLogic.ShowsGameplayRoot(tab) ? 1 : 0) + (GuideTabLogic.ShowsList(tab) ? 1 : 0);
                Assert.AreEqual(1, visible, tab.ToString());
            }
        }

        [Test]
        public void RowKinds_NeverShowTogether()
        {
            foreach (GuideTab tab in System.Enum.GetValues(typeof(GuideTab)))
            {
                Assert.IsFalse(GuideTabLogic.ShowsAugmentRows(tab) && GuideTabLogic.ShowsFairyRows(tab), tab.ToString());
            }
        }
    }
}
