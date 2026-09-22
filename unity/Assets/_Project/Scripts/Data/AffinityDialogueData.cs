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

    /// <summary>
    /// 캐릭터 하나가 가지는 호감도 대화.
    /// introLines → question1(증강 3택)은 런 시작 직전에, bossIntroLines는
    /// 5:00 보스전 진입 직전에 쓴다. 각각 비어 있으면 그 단계를 건너뛴다.
    /// </summary>
    [CreateAssetMenu(menuName = "SushiSurvival/Affinity Dialogue Data", fileName = "NewAffinityDialogueData")]
    public class AffinityDialogueData : ScriptableObject
    {
        [Tooltip("question1 앞에 순서대로 재생되는 자기소개 나레이션. 비우면 곧바로 question1이 뜬다.")]
        public StoryLine[] introLines;
        public AffinityDialogueQuestion question1;
        [Tooltip("보스전 진입 직전 재생되는 나레이션(선택지 없음). 비우면 인터럽트 없이 바로 보스전.")]
        public StoryLine[] bossIntroLines;
    }
}
