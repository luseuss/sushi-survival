using UnityEngine;

namespace SushiSurvival.Core
{
    public static class CameraFollowLogic
    {
        /// <summary>
        /// 카메라를 대상 쪽으로 factor(0~1)만큼 보간한 위치를 돌려준다.
        /// z는 항상 카메라의 기존 값을 유지한다 — 2D에서 카메라 z가 대상(보통 0)을
        /// 따라가버리면 화면이 통째로 비어 보이기 때문.
        /// </summary>
        public static Vector3 ComputeFollowPosition(Vector3 currentCameraPos, Vector3 targetPos, float factor)
        {
            float x = Mathf.Lerp(currentCameraPos.x, targetPos.x, factor);
            float y = Mathf.Lerp(currentCameraPos.y, targetPos.y, factor);
            return new Vector3(x, y, currentCameraPos.z);
        }
    }
}
