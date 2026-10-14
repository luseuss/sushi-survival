using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class SpawnPoolLogicTests
    {
        [Test]
        public void BeforeUnlock_OnlyEarlierPoolsAreAvailable()
        {
            var result = SpawnPoolLogic.AvailableIndices(59.9f, new float[] { 0f, 60f }, 2);
            CollectionAssert.AreEqual(new[] { 0 }, result);
        }

        [Test]
        public void AtUnlockTime_LaterPoolJoins()
        {
            var result = SpawnPoolLogic.AvailableIndices(60f, new float[] { 0f, 60f }, 2);
            CollectionAssert.AreEqual(new[] { 0, 1 }, result);
        }

        [Test]
        public void MissingUnlockValues_MeanAvailableFromStart()
        {
            CollectionAssert.AreEqual(new[] { 0, 1 }, SpawnPoolLogic.AvailableIndices(0f, null, 2));
            CollectionAssert.AreEqual(new[] { 0, 1 }, SpawnPoolLogic.AvailableIndices(0f, new float[] { 0f }, 2));
        }

        [Test]
        public void NothingUnlocked_FallsBackToFirstPool()
        {
            var result = SpawnPoolLogic.AvailableIndices(0f, new float[] { 30f, 60f }, 2);
            CollectionAssert.AreEqual(new[] { 0 }, result);
        }

        [Test]
        public void NoPools_ReturnsEmpty()
        {
            Assert.IsEmpty(SpawnPoolLogic.AvailableIndices(10f, null, 0));
        }
    }
}
