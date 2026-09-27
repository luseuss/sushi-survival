using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class PlayerTraitLinesTests
    {
        private PlayerTraitLines _lines;

        [SetUp]
        public void SetUp()
        {
            _lines = ScriptableObject.CreateInstance<PlayerTraitLines>();
            _lines.entries = new[]
            {
                new PlayerTraitEntry { trait = PlayerTrait.Bold, displayName = "용감함", victoryLine = "v", defeatLine = "d" },
                null,
                new PlayerTraitEntry { trait = PlayerTrait.Kind, displayName = "다정함", victoryLine = "v", defeatLine = "d" },
            };
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_lines);

        [Test]
        public void Find_ReturnsTheMatchingEntry()
        {
            Assert.AreEqual("다정함", _lines.Find(PlayerTrait.Kind).displayName);
        }

        [Test]
        public void Find_None_ReturnsNull()
        {
            Assert.IsNull(_lines.Find(PlayerTrait.None));
        }

        [Test]
        public void Find_MissingTrait_ReturnsNull()
        {
            Assert.IsNull(_lines.Find(PlayerTrait.Careful));
        }

        [Test]
        public void Find_NullEntries_ReturnsNull()
        {
            _lines.entries = null;
            Assert.IsNull(_lines.Find(PlayerTrait.Bold));
        }
    }
}
