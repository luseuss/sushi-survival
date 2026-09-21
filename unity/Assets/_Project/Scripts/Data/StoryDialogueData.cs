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
        [TextArea]
        public string text;
        [Tooltip("이 줄에서 바뀔 배경. 비우면 직전 배경을 유지한다.")]
        public Sprite background;
    }

    /// <summary>선형 대화 한 세트. StorySceneController가 순서대로 넘긴다.</summary>
    [CreateAssetMenu(menuName = "SushiSurvival/Story Dialogue Data", fileName = "NewStoryDialogueData")]
    public class StoryDialogueData : ScriptableObject
    {
        public StoryLine[] lines;
    }
}
