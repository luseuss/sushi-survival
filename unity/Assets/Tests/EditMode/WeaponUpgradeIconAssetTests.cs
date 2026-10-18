using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    /// <summary>레벨업 창의 무기 강화 카드가 아이콘 없이 나오지 않도록, 플레이어 프리팹의 무기마다 아이콘이 연결돼 있는지 확인한다.</summary>
    public class WeaponUpgradeIconAssetTests
    {
        [TestCase("Assets/_Project/Prefabs/EggPlayer.prefab")]
        [TestCase("Assets/_Project/Prefabs/ShrimpPlayer.prefab")]
        public void EveryWeaponOnThePlayerPrefab_HasAnUpgradeIcon(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, path);

            WeaponBase[] weapons = prefab.GetComponentsInChildren<WeaponBase>(true);
            Assert.IsNotEmpty(weapons, path + ": 무기 없음");

            foreach (WeaponBase weapon in weapons)
                Assert.IsNotNull(weapon.UpgradeIcon, path + ": " + weapon.GetType().Name + "에 무기 강화 아이콘이 없음");
        }
    }
}
