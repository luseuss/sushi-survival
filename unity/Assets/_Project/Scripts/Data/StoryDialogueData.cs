using System;
using UnityEngine;

namespace SushiSurvival.Data
{
    /// <summary>대사 한 줄. 호감도 대화와 달리 선택지·스탯 버프는 없다.</summary>
    [Serializable]
    public class StoryLine
    {
        [Tooltip("이름표에 표시. 비우면 이름표를 숨긴다(내레이션용).")]
        public string speakerName;
        [Tooltip("대사창 안 작은 초상화. 비우면 초상화 창을 숨긴다.")]
        public Sprite portrait;
        [Tooltip("화면에 크게 서 있는 입상 일러스트. 비우면 입상을 숨긴다.")]
        public Sprite standing;
        [TextArea]
        public string text;
        [Tooltip("이 줄에서 바뀔 배경. 비우면 직전 배경을 유지한다.")]
        public Sprite background;
    }

    /// <summary>질문의 선택지 하나. 고르면 성향이 쌓이고 반응 대사 한 줄이 나온다.</summary>
    [Serializable]
    public class StoryChoice
    {
        [TextArea]
        [Tooltip("버튼에 표시할 문구.")]
        public string choiceText;
        [Tooltip("이 선택이 쌓는 성향.")]
        public PlayerTrait trait;
        [Tooltip("고른 직후 나오는 반응 대사 한 줄. 비우면 반응 없이 바로 다음 대사로 넘어간다.")]
        public StoryLine reply;
    }

    /// <summary>
    /// 선형 대화 중간에 끼는 질문 하나. StoryLine이 자기 자신을 품으면 Unity 직렬화가 깨져서
    /// 질문은 이렇게 별도 클래스로 둔다.
    /// </summary>
    [Serializable]
    public class StoryChoicePoint
    {
        [Tooltip("이 번호의 대사 줄 바로 앞에 질문을 끼운다(0부터). 같은 번호에 여러 개면 첫 번째만 나온다.")]
        public int beforeLineIndex;
        [Tooltip("질문 대사. 화자·초상화·입상을 지정할 수 있다.")]
        public StoryLine prompt;
        [Tooltip("2~3개.")]
        public StoryChoice[] choices;
    }

    /// <summary>선형 대화 한 세트. StorySceneController가 순서대로 넘긴다.</summary>
    [CreateAssetMenu(menuName = "SushiSurvival/Story Dialogue Data", fileName = "NewStoryDialogueData")]
    public class StoryDialogueData : ScriptableObject
    {
        public StoryLine[] lines;
        [Tooltip("대화 중간에 끼는 질문. 비우면 질문 없이 선형으로만 재생한다.")]
        public StoryChoicePoint[] choicePoints;
    }
}
