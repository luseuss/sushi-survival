using UnityEngine;

namespace SushiSurvival.Data
{
    [System.Serializable]
    public struct WeaponLevelStats
    {
        public float damage;
        public float cooldown;
        public float range;
        [Tooltip("근접 무기 전용 (부채꼴 전체각, 도)")]
        public float angleDegrees;
        [Tooltip("원거리 무기 전용 (관통 수)")]
        public int pierceCount;
    }

    [CreateAssetMenu(menuName = "SushiSurvival/Weapon Data", fileName = "NewWeaponData")]
    public class WeaponData : ScriptableObject
    {
        public string weaponName;
        public bool isMelee = true;
        [Tooltip("원거리 무기 전용, 근접이면 비워둔다")]
        public GameObject projectilePrefab;
        [Tooltip("인덱스 0 = Lv1 ... 인덱스 3 = Lv4(MAX)")]
        public WeaponLevelStats[] levels = new WeaponLevelStats[4];
    }
}
