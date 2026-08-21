using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class ObjectPoolTests
    {
        private class DummyItem
        {
            public bool Active;
        }

        [Test]
        public void Get_CreatesNewInstance_WhenPoolIsEmpty()
        {
            int createCount = 0;
            var pool = new ObjectPool<DummyItem>(() => { createCount++; return new DummyItem(); });

            pool.Get();

            Assert.AreEqual(1, createCount);
        }

        [Test]
        public void Get_ReusesReleasedInstance_InsteadOfCreatingNew()
        {
            int createCount = 0;
            var pool = new ObjectPool<DummyItem>(() => { createCount++; return new DummyItem(); });

            var item = pool.Get();
            pool.Release(item);
            var reused = pool.Get();

            Assert.AreEqual(1, createCount);
            Assert.AreSame(item, reused);
        }

        [Test]
        public void Get_InvokesOnGetCallback()
        {
            var pool = new ObjectPool<DummyItem>(() => new DummyItem(), onGet: i => i.Active = true);

            var item = pool.Get();

            Assert.IsTrue(item.Active);
        }

        [Test]
        public void Release_InvokesOnReleaseCallback()
        {
            var pool = new ObjectPool<DummyItem>(() => new DummyItem(), onRelease: i => i.Active = false);
            var item = pool.Get();
            item.Active = true;

            pool.Release(item);

            Assert.IsFalse(item.Active);
        }

        [Test]
        public void InactiveCount_TracksReleasedItems()
        {
            var pool = new ObjectPool<DummyItem>(() => new DummyItem());
            var item = pool.Get();

            pool.Release(item);

            Assert.AreEqual(1, pool.InactiveCount);
        }
    }
}
