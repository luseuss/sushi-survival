using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class AugmentTallyTests
    {
        private static AugmentData MakeAugment(string augmentName)
        {
            var data = ScriptableObject.CreateInstance<AugmentData>();
            data.augmentName = augmentName;
            return data;
        }

        [Test]
        public void Summarize_ReturnsEmpty_ForEmptyInput()
        {
            var result = AugmentTally.Summarize(new List<AugmentData>());

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Summarize_CountsSinglePickAsOne()
        {
            var attack = MakeAugment("공격력");

            var result = AugmentTally.Summarize(new List<AugmentData> { attack });

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, result[0].Count);
            Assert.AreSame(attack, result[0].Data);
        }

        [Test]
        public void Summarize_GroupsRepeatedPicks()
        {
            var attack = MakeAugment("공격력");

            var result = AugmentTally.Summarize(new List<AugmentData> { attack, attack, attack });

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(3, result[0].Count);
        }

        [Test]
        public void Summarize_KeepsDistinctAugmentsSeparate()
        {
            var attack = MakeAugment("공격력");
            var armor = MakeAugment("방어력");

            var result = AugmentTally.Summarize(new List<AugmentData> { attack, armor, attack });

            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void Summarize_PreservesFirstPickOrder()
        {
            var attack = MakeAugment("공격력");
            var armor = MakeAugment("방어력");

            var result = AugmentTally.Summarize(new List<AugmentData> { armor, attack, armor });

            Assert.AreSame(armor, result[0].Data);
            Assert.AreSame(attack, result[1].Data);
        }
    }
}
