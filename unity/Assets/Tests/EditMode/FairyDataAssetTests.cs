using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    /// <summary>펫 9종 데이터가 구조(프레임·레벨 수·이름)대로 들어갔는지 확인한다. 수치 자체는 밸런스로 바뀌므로 보지 않는다.</summary>
    public class FairyDataAssetTests
    {
        private const string Folder = "Assets/_Project/Data/Fairies";

        private static List<FairyData> LoadAll()
        {
            return AssetDatabase.FindAssets("t:FairyData", new[] { Folder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<FairyData>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();
        }

        [Test]
        public void Folder_HasNinePets()
        {
            Assert.AreEqual(9, LoadAll().Count);
        }

        [Test]
        public void EveryPet_HasNameAndRole()
        {
            foreach (FairyData data in LoadAll())
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(data.displayName), data.name);
                Assert.IsFalse(string.IsNullOrWhiteSpace(data.roleLine), data.name);
            }
        }

        [Test]
        public void EveryPet_HasFiveFramesWithoutGaps()
        {
            foreach (FairyData data in LoadAll())
            {
                Assert.AreEqual(5, data.frames.Length, data.name);
                foreach (var frame in data.frames)
                    Assert.IsNotNull(frame, $"{data.name}에 빈 프레임이 있습니다.");
            }
        }

        [Test]
        public void EveryPet_HasFourLevelsWithPositiveStats()
        {
            foreach (FairyData data in LoadAll())
            {
                Assert.AreEqual(4, data.levels.Length, data.name);
                foreach (var level in data.levels)
                {
                    Assert.Greater(level.damage, 0f, data.name);
                    Assert.Greater(level.cooldown, 0f, data.name);
                    Assert.Greater(level.range, 0f, data.name);
                }
            }
        }

        [Test]
        public void PetNames_AreUnique()
        {
            List<FairyData> all = LoadAll();

            Assert.AreEqual(all.Count, all.Select(d => d.displayName).Distinct().Count());
        }

        [Test]
        public void Stats_GrowAcrossLevels()
        {
            foreach (FairyData data in LoadAll())
            {
                Assert.Greater(data.levels[3].damage, data.levels[0].damage, data.name);
                Assert.Less(data.levels[3].cooldown, data.levels[0].cooldown, data.name);
            }
        }
    }
}
