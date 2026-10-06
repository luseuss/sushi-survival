using System.Collections.Generic;
using NUnit.Framework;
using SushiSurvival.Companions;

namespace SushiSurvival.EditModeTests
{
    public class FairyChoiceLogicTests
    {
        [Test]
        public void NoFairies_OffersOnlySummon()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(new int[0], 3, 4);

            Assert.AreEqual(1, choices.Count);
            Assert.AreEqual(FairyChoiceKind.Summon, choices[0].Kind);
            Assert.AreEqual(-1, choices[0].Index);
        }

        [Test]
        public void OneFairy_OffersSummonThenUpgrade()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(new[] { 2 }, 3, 4);

            Assert.AreEqual(2, choices.Count);
            Assert.AreEqual(FairyChoiceKind.Summon, choices[0].Kind);
            Assert.AreEqual(FairyChoiceKind.Upgrade, choices[1].Kind);
            Assert.AreEqual(0, choices[1].Index);
        }

        [Test]
        public void TwoFairies_SummonPlusTwoUpgrades_IsThreeCards()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(new[] { 1, 3 }, 3, 4);

            Assert.AreEqual(3, choices.Count);
            Assert.AreEqual(FairyChoiceKind.Summon, choices[0].Kind);
            Assert.AreEqual(0, choices[1].Index);
            Assert.AreEqual(1, choices[2].Index);
        }

        [Test]
        public void ThreeFairies_OnlyUpgrades_NeverMoreThanThree()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(new[] { 1, 1, 1 }, 3, 4);

            Assert.AreEqual(3, choices.Count);
            foreach (FairyChoice choice in choices)
                Assert.AreEqual(FairyChoiceKind.Upgrade, choice.Kind);
        }

        [Test]
        public void MaxLevelFairy_IsNotOfferedAnUpgrade()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(new[] { 4, 2, 4 }, 3, 4);

            Assert.AreEqual(1, choices.Count);
            Assert.AreEqual(FairyChoiceKind.Upgrade, choices[0].Kind);
            Assert.AreEqual(1, choices[0].Index);
        }

        [Test]
        public void EverythingMaxed_ReturnsEmpty()
        {
            Assert.IsEmpty(FairyChoiceLogic.Build(new[] { 4, 4, 4 }, 3, 4));
        }

        [Test]
        public void NullLevels_IsTreatedAsNoFairies()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(null, 3, 4);

            Assert.AreEqual(1, choices.Count);
            Assert.AreEqual(FairyChoiceKind.Summon, choices[0].Kind);
        }

        [Test]
        public void LargerMaxCount_StillCapsAtThreeCards()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(new[] { 1, 1, 1, 1 }, 6, 4);

            Assert.AreEqual(FairyChoiceLogic.MaxChoices, choices.Count);
        }
    }
}
