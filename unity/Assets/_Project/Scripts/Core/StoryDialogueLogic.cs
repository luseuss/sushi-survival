using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 선형 대화(StoryDialogueData)의 진행 판정. Unity 오브젝트에 의존하지 않도록
    /// "이 줄이 배경을 지정하는가"만 bool 목록으로 받는다.
    /// </summary>
    public static class StoryDialogueLogic
    {
        /// <summary>다음 줄 번호. 끝을 넘으면 count를 돌려준다(그 값이 "끝"이다).</summary>
        public static int NextIndex(int current, int count)
        {
            int safeCount = Mathf.Max(0, count);
            return Mathf.Clamp(current + 1, 0, safeCount);
        }

        /// <summary>줄이 하나도 없거나 index가 끝에 닿았으면 true.</summary>
        public static bool IsFinished(int index, int count)
            => count <= 0 || index >= count;

        /// <summary>
        /// index 이하에서 가장 가까운 "배경을 지정한 줄"의 번호. 배경이 비어 있는 줄은
        /// 직전 배경을 유지하므로, 지금 화면에 깔려 있어야 할 배경이 어느 줄 것인지 알려준다.
        /// 없거나 입력이 잘못됐으면 -1.
        /// </summary>
        public static int ResolveBackgroundIndex(IReadOnlyList<bool> hasBackground, int index)
        {
            if (hasBackground == null || hasBackground.Count == 0 || index < 0) return -1;

            for (int i = Mathf.Min(index, hasBackground.Count - 1); i >= 0; i--)
            {
                if (hasBackground[i]) return i;
            }

            return -1;
        }

        /// <summary>이 줄에서 배경이 새로 바뀌는가(= 이 줄이 배경을 지정했는가).</summary>
        public static bool IsBackgroundChange(IReadOnlyList<bool> hasBackground, int index)
            => hasBackground != null && index >= 0 && index < hasBackground.Count && hasBackground[index];
    }
}
