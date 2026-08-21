using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 카메라가 플레이어를 부드럽게 따라간다. 캐릭터 종류와 무관하므로
    /// 계란·간장새우·이나리 모두 그대로 재사용한다.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Tooltip("비워두면 시작할 때 Player 태그로 찾고, 그래도 없으면 " +
                 "GameManager가 스폰 후 SetTarget으로 알려줄 때까지 기다린다.")]
        [SerializeField] private Transform target;
        [Tooltip("클수록 빠르게 따라붙는다. 0이면 따라가지 않는다.")]
        [SerializeField] private float followSpeed = 5f;

        public void SetTarget(Transform newTarget) => target = newTarget;

        private void Start()
        {
            if (target != null) return;

            // 캐릭터 선택 화면에서는 아직 플레이어가 없는 것이 정상이므로
            // 못 찾아도 에러를 내지 않는다.
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float factor = followSpeed * Time.deltaTime;
            transform.position = CameraFollowLogic.ComputeFollowPosition(
                transform.position, target.position, factor);
        }
    }
}
