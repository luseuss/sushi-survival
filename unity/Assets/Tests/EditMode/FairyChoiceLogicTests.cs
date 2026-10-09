using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SushiSurvival.Companions;

namespace SushiSurvival.EditModeTests
{
    public class FairyChoiceLogicTests
    {
        private const int Catalog = 9;
        private static readonly int[] None = new int[0];

        private static List<FairyChoice> Build(int[] owned, int[] levels, int seed = 1, int catalog = Catalog)
            => FairyChoiceLogic.Build(owned, levels, catalog, 3, 4, new Random(seed));

        [Test]
        public void NoFairies_OffersThreeDistinctSummons()
        {
            List<FairyChoice> choices = Build(None, None);

            Assert.AreEqual(3, choices.Count);
            foreach (FairyChoice choice in choices)
            {
                Assert.AreEqual(FairyChoiceKind.Summon, choice.Kind);
                Assert.AreEqual(-1, choice.Index);
                Assert.That(choice.KindIndex, Is.InRange(0, Catalog - 1));
            }
            Assert.AreEqual(3, choices.Select(c => c.KindIndex).Distinct().Count());
        }

        [Test]
        public void OwnedKinds_AreNeverOffered()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                List<FairyChoice> choices = Build(new[] { 0, 4 }, new[] { 1, 2 }, seed);

                Assert.AreEqual(3, choices.Count);
                foreach (FairyChoice choice in choices)
                {
                    Assert.AreNotEqual(0, choice.KindIndex);
                    Assert.AreNotEqual(4, choice.KindIndex);
                }
            }
        }

        [Test]
        public void FewUnownedKinds_OffersOnlyThatMany()
        {
            List<FairyChoice> choices = Build(new[] { 0, 1 }, new[] { 1, 1 }, 1, 3);

            Assert.AreEqual(1, choices.Count);
            Assert.AreEqual(2, choices[0].KindIndex);
        }

        [Test]
        public void EmptyCatalog_ReturnsEmpty()
        {
            Assert.IsEmpty(Build(None, None, 1, 0));
        }

        [Test]
        public void SameSeed_GivesSameChoices()
        {
            List<int> a = Build(None, None, 7).Select(c => c.KindIndex).ToList();
            List<int> b = Build(None, None, 7).Select(c => c.KindIndex).ToList();

            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentChoicesSometimes()
        {
            var firsts = new HashSet<string>();
            for (int seed = 0; seed < 20; seed++)
                firsts.Add(string.Join(",", Build(None, None, seed).Select(c => c.KindIndex)));

            Assert.Greater(firsts.Count, 1);
        }

        [Test]
        public void NullRandom_UsesCatalogOrder()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(new[] { 1 }, new[] { 1 }, Catalog, 3, 4, null);

            CollectionAssert.AreEqual(new[] { 0, 2, 3 }, choices.Select(c => c.KindIndex).ToArray());
        }

        [Test]
        public void Full_OffersOnlyUpgrades_NeverMoreThanThree()
        {
            List<FairyChoice> choices = Build(new[] { 0, 1, 2 }, new[] { 1, 1, 1 });

            Assert.AreEqual(3, choices.Count);
            for (int i = 0; i < choices.Count; i++)
            {
                Assert.AreEqual(FairyChoiceKind.Upgrade, choices[i].Kind);
                Assert.AreEqual(i, choices[i].Index);
                Assert.AreEqual(-1, choices[i].KindIndex);
            }
        }

        [Test]
        public void Full_MaxLevelFairy_IsNotOfferedAnUpgrade()
        {
            List<FairyChoice> choices = Build(new[] { 0, 1, 2 }, new[] { 4, 2, 4 });

            Assert.AreEqual(1, choices.Count);
            Assert.AreEqual(1, choices[0].Index);
        }

        [Test]
        public void Full_EverythingMaxed_ReturnsEmpty()
        {
            Assert.IsEmpty(Build(new[] { 0, 1, 2 }, new[] { 4, 4, 4 }));
        }

        [Test]
        public void NotFull_NeverMixesUpgradesIn()
        {
            List<FairyChoice> choices = Build(new[] { 0 }, new[] { 1 });

            foreach (FairyChoice choice in choices)
                Assert.AreEqual(FairyChoiceKind.Summon, choice.Kind);
        }

        [Test]
        public void NullLists_AreTreatedAsNoFairies()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(null, null, Catalog, 3, 4, new Random(1));

            Assert.AreEqual(3, choices.Count);
        }
    }
}
