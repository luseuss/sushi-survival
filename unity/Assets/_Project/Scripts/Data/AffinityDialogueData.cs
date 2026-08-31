using UnityEngine;

namespace SushiSurvival.Data
{
    /// <summary>선택지 하나. 서사용 대사와, 그 대사가 매핑되는 증강을 함께 든다.</summary>
    [System.Serializable]
    public class AffinityDialogueChoice
    {
        [TextArea]
        public string choiceText;
        [Tooltip("이 선택이 매핑되는 증강. 이름·아이콘·StatType·maxCap을 여기서 가져온다.")]
        public AugmentData augment;
    }

    /// <summary>질문 하나 + 선택지 2~3개.</summary>
    [System.Serializable]
    public class AffinityDialogueQuestion
    {
        [TextArea]
        public string questionText;
        [Tooltip("2~3개.")]
        public AffinityDialogueChoice[] choices;
    }

    /// <summary>캐릭터 하나가 가지는 호감도 대화. #1만 다룬다(#2는 별도 슬라이스).</summary>
    [CreateAssetMenu(menuName = "SushiSurvival/Affinity Dialogue Data", fileName = "NewAffinityDialogueData")]
    public class AffinityDialogueData : ScriptableObject
    {
        public AffinityDialogueQuestion question1;
    }
}
