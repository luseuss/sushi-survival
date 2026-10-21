using UnityEngine;

namespace SushiSurvival.Enemies
{
    /// <summary>
    /// 몹이 플레이어를 쫓아 가는 방향에 맞춰 그림을 좌우로 뒤집는다. EnemyAI의 추격 방향을 읽으므로
    /// 다른 몹에게 막혀 비껴 가거나 넉백을 받아도 얼굴이 이리저리 돌지 않는다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyFlip : MonoBehaviour
    {
        [Tooltip("그림이 오른쪽을 보고 그려져 있으면 켠다(왼쪽으로 갈 때 뒤집힘). 왼쪽을 보고 그려져 있으면 끈다.")]
        [SerializeField] private bool artFacesRight = true;
        [Tooltip("이동 방향의 가로 성분(-1~1)이 이보다 작으면 방향을 바꾸지 않는다. 거의 위아래로 갈 때 깜빡이는 걸 막는다.")]
        [SerializeField] private float deadZone = 0.15f;

        private SpriteRenderer _renderer;
        private EnemyAI _ai;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _ai = GetComponent<EnemyAI>();
        }

        // 풀에서 재사용되므로 이전 개체가 뒤집힌 채 돌아왔을 수 있다 — 첫 프레임에 방향이 정해질 때까지 기본 방향으로 둔다.
        private void OnEnable() => _renderer.flipX = false;

        private void LateUpdate()
        {
            _renderer.flipX = EnemyFlipLogic.ShouldFlip(_renderer.flipX, _ai.ChaseDirection.x, deadZone, artFacesRight);
        }
    }
}
