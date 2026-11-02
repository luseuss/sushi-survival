using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 프레임 그림 연출의 값 계산 — 경과 시간과 초당 프레임 수로 지금 보일 프레임 번호를 돌려준다.
    /// 오브젝트에 적용하는 일은 UiFrameAnimator가 한다.
    /// </summary>
    public static class FrameAnimationLogic
    {
        /// <summary>
        /// elapsed초 시점에 보일 프레임 번호(0 ~ frameCount-1). 반복하면 처음으로 돌아가고, 아니면 마지막 프레임에 머문다.
        /// 프레임이 없거나 fps가 0 이하이면 0.
        /// </summary>
        public static int FrameAt(float elapsed, float fps, int frameCount, bool loop)
        {
            if (frameCount <= 0 || fps <= 0f) return 0;

            int index = Mathf.FloorToInt(Mathf.Max(0f, elapsed) * fps);
            if (loop) return index % frameCount;
            return Mathf.Min(index, frameCount - 1);
        }
    }
}
