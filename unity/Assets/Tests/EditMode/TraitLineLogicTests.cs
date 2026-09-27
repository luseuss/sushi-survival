using NUnit.Framework;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class TraitLineLogicTests
    {
        private static StoryLine Line(string text) => new StoryLine { text = text };

        private static TraitLine Trait(PlayerTrait trait, string text)
            => new TraitLine { trait = trait, line = Line(text) };

        [Test]
        public void AppendsTheDominantTraitLineAtTheEnd()
        {
            var baseLines = new[] { Line("a"), Line("b") };
            var traitLines = new[] { Trait(PlayerTrait.Bold, "bold"), Trait(PlayerTrait.Kind, "kind") };

            StoryLine[] result = TraitLineLogic.AppendTraitLine(baseLines, traitLines, PlayerTrait.Kind);

            Assert.AreEqual(3, result.Length);
            Assert.AreEqual("kind", result[2].text);
            Assert.AreEqual("a", result[0].text);
        }

        [Test]
        public void DoesNotChangeTheOriginalArray()
        {
            var baseLines = new[] { Line("a") };

            TraitLineLogic.AppendTraitLine(baseLines, new[] { Trait(PlayerTrait.Bold, "bold") }, PlayerTrait.Bold);

            Assert.AreEqual(1, baseLines.Length);
        }

        [Test]
        public void NoMatchingTrait_ReturnsTheSameArray()
        {
            var baseLines = new[] { Line("a") };

            StoryLine[] result = TraitLineLogic.AppendTraitLine(
                baseLines, new[] { Trait(PlayerTrait.Bold, "bold") }, PlayerTrait.Kind);

            Assert.AreSame(baseLines, result);
        }

        [Test]
        public void DominantNone_ReturnsTheSameArray()
        {
            var baseLines = new[] { Line("a") };

            StoryLine[] result = TraitLineLogic.AppendTraitLine(
                baseLines, new[] { Trait(PlayerTrait.Bold, "bold") }, PlayerTrait.None);

            Assert.AreSame(baseLines, result);
        }

        [Test]
        public void EmptyOrNullBaseLines_AreReturnedAsIs_SoATraitLineNeverPlaysAlone()
        {
            var empty = new StoryLine[0];
            var traitLines = new[] { Trait(PlayerTrait.Bold, "bold") };

            Assert.AreSame(empty, TraitLineLogic.AppendTraitLine(empty, traitLines, PlayerTrait.Bold));
            Assert.IsNull(TraitLineLogic.AppendTraitLine(null, traitLines, PlayerTrait.Bold));
        }

        [Test]
        public void NullTraitLines_OrNullEntries_AreIgnored()
        {
            var baseLines = new[] { Line("a") };

            Assert.AreSame(baseLines, TraitLineLogic.AppendTraitLine(baseLines, null, PlayerTrait.Bold));

            var withNulls = new TraitLine[] { null, new TraitLine { trait = PlayerTrait.Bold, line = null } };
            Assert.AreSame(baseLines, TraitLineLogic.AppendTraitLine(baseLines, withNulls, PlayerTrait.Bold));
        }
    }
}
