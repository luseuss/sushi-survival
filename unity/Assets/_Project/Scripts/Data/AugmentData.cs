using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.Data
{
    [CreateAssetMenu(menuName = "SushiSurvival/Augment Data", fileName = "NewAugmentData")]
    public class AugmentData : ScriptableObject
    {
        public string augmentName;
        public Sprite icon;
        public StatType statType;
        [Tooltip("한 번 고를 때마다 더해지는 값. 배율 스탯이면 0.2 = +20%.")]
        public float valuePerPick;
        [Tooltip("누적 상한. 기획서의 추천 최대치를 넣는다(공격력 +200%면 2.0).")]
        public float maxCap;
    }
}
