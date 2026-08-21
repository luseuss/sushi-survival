using System.Collections.Generic;
using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class UpgradePickerTests
    {
        [Test]
        public void PickDistinct_ReturnsRequestedCount()
        {
            var candidates = new List<string> { "a", "b", "c", "d", "e" };

            var result = UpgradePicker.PickDistinct(candidates, 3, new System.Random(1));

            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void PickDistinct_ReturnsNoDuplicates()
        {
            var candidates = new List<string> { "a", "b", "c", "d", "e" };

            var result = UpgradePicker.PickDistinct(candidates, 3, new System.Random(1));

            CollectionAssert.AllItemsAreUnique(result);
        }

        [Test]
        public void PickDistinct_ReturnsAll_WhenFewerCandidatesThanRequested()
        {
            var candidates = new List<string> { "a", "b" };

            var result = UpgradePicker.PickDistinct(candidates, 3, new System.Random(1));

            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void PickDistinct_ReturnsEmpty_WhenNoCandidates()
        {
            var result = UpgradePicker.PickDistinct(new List<string>(), 3, new System.Random(1));

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void PickDistinct_DoesNotModifySourceList()
        {
            var candidates = new List<string> { "a", "b", "c", "d", "e" };

            UpgradePicker.PickDistinct(candidates, 3, new System.Random(1));

            Assert.AreEqual(5, candidates.Count);
        }
    }
}
