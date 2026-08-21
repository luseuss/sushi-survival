using UnityEngine;

namespace SushiSurvival.Data
{
    public enum XPGemType
    {
        Basic,
        Five,
        Ten
    }

    [CreateAssetMenu(menuName = "SushiSurvival/Monster Data", fileName = "NewMonsterData")]
    public class MonsterData : ScriptableObject
    {
        public string monsterName;
        public float maxHealth = 12f;
        public float contactDamage = 5f;
        public float moveSpeed = 2f;
        [Range(0f, 1f)]
        [Tooltip("피격 시 밀려나는 정도를 줄인다. 0이면 그대로 밀리고 1이면 꿈쩍도 않는다. 중형몹·보스는 높게.")]
        public float knockbackResistance;
        public XPGemType xpGemDrop = XPGemType.Basic;

        [Tooltip("켜면 경과 시간에 따라 체력이 불어난다. 잡몹만 켠다 — " +
                 "중형몹·보스는 설계된 고정 체력이라 배율이 곱해지면 안 된다.")]
        public bool scalesWithTime;

        [Tooltip("이 시각(초) 이후로는 아래 등급 젬을 떨어뜨린다. 0이면 승급 없음. 3:00 = 180")]
        public float gemUpgradeTime;
        [Tooltip("승급 후 떨어뜨릴 젬 등급.")]
        public XPGemType upgradedGemDrop = XPGemType.Five;

        public RuntimeAnimatorController animatorController;
    }
}
