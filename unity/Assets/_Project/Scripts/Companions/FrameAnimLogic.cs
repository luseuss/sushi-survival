using UnityEngine;

namespace SushiSurvival.Companions
{
    public static class FrameAnimLogic
    {
        /// <summary>시간(초)에 맞는 프레임 번호. 끝에 닿으면 처음으로 돌아가 반복한다.</summary>
        public static int FrameIndex(float time, float framesPerSecond, int frameCount)
        {
            if (frameCount <= 0 || framesPerSecond <= 0f || time <= 0f) return 0;

            return Mathf.FloorToInt(time * framesPerSecond) % frameCount;
        }
    }
}
