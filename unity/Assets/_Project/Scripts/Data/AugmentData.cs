using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.Data
{
    [CreateAssetMenu(menuName = "SushiSurvival/Augment Data", fileName = "NewAugmentData")]
    public class AugmentData : ScriptableObject
    {
        public string augmentName;
        [TextArea]
        [Tooltip("카드 제목 아래에 적을 설명. 비우면 스탯과 한 번에 오르는 값으로 자동 생성한다(예: 공격력 +20%).")]
        public string description;
        public Sprite icon;
        public StatType statType;
        [Tooltip("한 번 고를 때마다 더해지는 값. 배율 스탯이면 0.2 = +20%.")]
        public float valuePerPick;
        [Tooltip("누적 상한. 기획서의 추천 최대치를 넣는다(공격력 +200%면 2.0).")]
        public float maxCap;
    }
}
