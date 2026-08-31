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

        // 흔들리지 않는 실제 추적 위치. transform.position은 여기에 흔들림
        // 오프셋을 얹은 값이라, 흔들림이 다음 프레임의 추적 기준을 오염시키지 않는다.
        private Vector3 _basePosition;
        private Vector2 _shakeOffset;

        public void SetTarget(Transform newTarget) => target = newTarget;

        /// <summary>JuiceDirector가 매 프레임 흔들림 오프셋을 여기로 밀어넣는다.</summary>
        public void SetShakeOffset(Vector2 offset) => _shakeOffset = offset;

        private void Awake()
        {
            _basePosition = transform.position;
        }

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
            _basePosition = CameraFollowLogic.ComputeFollowPosition(_basePosition, target.position, factor);
            transform.position = _basePosition + (Vector3)_shakeOffset;
        }
    }
}
