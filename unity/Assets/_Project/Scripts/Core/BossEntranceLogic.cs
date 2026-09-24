using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>보스가 하늘에서 떨어져 등장하는 연출의 순수 계산. 시간 진행과 화면 반영은 BossFightDirector가 맡는다.</summary>
    public static class BossEntranceLogic
    {
        /// <summary>카메라가 착지점을 비추고 있을 때 화면 위 끝 밖에서 출발하도록 낙하 시작 높이를 구한다.</summary>
        public static float DropStartHeight(float orthographicSize, float margin)
            => Mathf.Max(0f, orthographicSize) + Mathf.Max(0f, margin);

        public static float Progress(float elapsed, float duration)
            => duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);

        /// <summary>중력처럼 처음엔 느리게, 끝에서 빠르게 떨어진다.</summary>
        public static float EaseIn(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }

        public static Vector3 DropPosition(Vector3 landing, float startHeight, float t)
            => landing + Vector3.up * (startHeight * (1f - EaseIn(t)));

        /// <summary>착지 충격으로 화면이 흔들리는 정도. 시간이 지날수록 줄어 duration이 되면 0이다.</summary>
        public static Vector2 ShakeOffset(float elapsed, float duration, float magnitude, Vector2 randomUnit)
        {
            if (duration <= 0f || elapsed >= duration) return Vector2.zero;
            return randomUnit * (magnitude * (1f - Progress(elapsed, duration)));
        }
    }
}
