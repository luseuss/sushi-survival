using NUnit.Framework;
using UnityEditor;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    /// <summary>우산 WeaponData 에셋이 4레벨 다 채워져 있고 스펙 시작값과 맞는지 확인한다.</summary>
    public class EggUmbrellaWeaponDataAssetTests
    {
        private const string AssetPath = "Assets/_Project/Data/EggUmbrellaWeaponData.asset";

        private static WeaponData Load()
        {
            var data = AssetDatabase.LoadAssetAtPath<WeaponData>(AssetPath);
            Assert.IsNotNull(data, $"{AssetPath}를 불러올 수 없습니다.");
            return data;
        }

        [Test]
        public void HasFourLevels()
        {
            Assert.AreEqual(4, Load().levels.Length);
        }

        [Test]
        public void Level1_MatchesSpecStartingValues()
        {
            var lv1 = Load().levels[0];
            Assert.AreEqual(8f, lv1.damage, 0.01f);
            Assert.AreEqual(0.3f, lv1.cooldown, 0.01f);
            Assert.AreEqual(1.6f, lv1.range, 0.01f);
        }

        [Test]
        public void AngleAndPierceAreUnusedForUmbrella()
        {
            foreach (var level in Load().levels)
            {
                Assert.AreEqual(0f, level.angleDegrees, 0.01f);
                Assert.AreEqual(0, level.pierceCount);
            }
        }

        [Test]
        public void DamageCooldownRange_ImproveEachLevel()
        {
            var levels = Load().levels;
            for (int i = 1; i < levels.Length; i++)
            {
                Assert.Greater(levels[i].damage, levels[i - 1].damage, $"Lv{i + 1} 피해가 Lv{i}보다 커야 합니다.");
                Assert.Less(levels[i].cooldown, levels[i - 1].cooldown, $"Lv{i + 1} 재타격 간격이 Lv{i}보다 짧아야 합니다.");
                Assert.GreaterOrEqual(levels[i].range, levels[i - 1].range, $"Lv{i + 1} 궤도 반경이 Lv{i}보다 같거나 커야 합니다.");
            }
        }
    }
}
