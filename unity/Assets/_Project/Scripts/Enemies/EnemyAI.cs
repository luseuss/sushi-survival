using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Data;

namespace SushiSurvival.Enemies
{
    /// <summary>
    /// 적의 유일한 이동 주체. 추격·분리·넉백 세 벡터를 합쳐 한 번만 움직인다.
    /// MovePosition을 부르는 곳이 둘 이상이면 서로 덮어쓰므로 여기서만 호출한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAI : MonoBehaviour
    {
        private const int MaxNeighbors = 8;

        // FixedUpdate는 단일 스레드이고 결과를 즉시 소비하므로 공유해도 안전하다.
        private static readonly Collider2D[] NeighborBuffer = new Collider2D[MaxNeighbors];

        [SerializeField] private MonsterData monsterData;
        [Tooltip("다른 적을 찾을 레이어. Enemy 레이어를 지정한다.")]
        [SerializeField] private LayerMask enemyLayer;
        [Tooltip("이 반경 안의 다른 적에게서 밀려난다.")]
        [SerializeField] private float separationRadius = 0.6f;
        [Tooltip("이동속도 대비 분리 힘의 배율.")]
        [SerializeField] private float separationStrength = 1.5f;
        [Tooltip("분리 벡터 갱신 주기(초). 매 프레임 물리 쿼리를 돌리지 않기 위함.")]
        [SerializeField] private float separationInterval = 0.1f;

        private readonly List<Vector2> _neighborPositions = new List<Vector2>(MaxNeighbors);

        [Tooltip("플레이어와 겹치는 정도(월드 단위). 살짝 겹쳐 있어야 접촉 데미지가 계속 들어온다.")]
        [SerializeField] private float contactOverlap = 0.02f;

        private Collider2D _ownCollider;
        private Collider2D _targetCollider;
        private Rigidbody2D _rigidbody;
        private EnemyBase _enemy;
        private Transform _target;
        private Vector2 _separation;
        private float _separationTimer;

        /// <summary>
        /// 추격 속도 배율. 0이면 제자리에 선다(보스 시전 중), 1.3이면 가속한다
        /// (보스 2페이즈). 넉백은 이 값과 무관하게 그대로 적용된다 — 시전 중에도
        /// 총에 맞으면 조금은 밀려야 타격감이 산다.
        /// </summary>
        public float MoveScale { get; set; } = 1f;

        /// <summary>
        /// 0이 아니면 플레이어를 쫓지 않고 이 방향(단위 벡터)으로만 간다. 보스 돌진이 방향을 고정할 때
        /// 쓴다 — 돌진 중에도 매 프레임 플레이어를 향하면 돌진이 아니라 그냥 빠른 추격이 된다.
        /// </summary>
        public Vector2 LockedDirection { get; set; }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _enemy = GetComponent<EnemyBase>();
            _ownCollider = GetComponent<Collider2D>();
        }

        private void OnEnable()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            _target = playerObj != null ? playerObj.transform : null;
            _targetCollider = playerObj != null ? playerObj.GetComponent<Collider2D>() : null;

            // 풀에서 재사용되므로 이전 판의 상태를 지운다.
            _separation = Vector2.zero;
            _separationTimer = 0f;
            MoveScale = 1f;
            LockedDirection = Vector2.zero;
        }

        private void FixedUpdate()
        {
            if (_target == null) return;

            UpdateSeparation();

            Vector2 chaseDirection = LockedDirection != Vector2.zero
                ? LockedDirection
                : ((Vector2)_target.position - _rigidbody.position).normalized;
            Vector2 chase = chaseDirection
                            * (monsterData.moveSpeed * MoveScale);
            Vector2 separation = _separation * (monsterData.moveSpeed * separationStrength);
            Vector2 knockback = _enemy != null ? _enemy.KnockbackVelocity : Vector2.zero;

            Vector2 move = (chase + separation + knockback) * Time.fixedDeltaTime;
            _rigidbody.MovePosition(BlockAgainstPlayer(_rigidbody.position + move));
        }

        /// <summary>
        /// 플레이어 몸 안으로 파고들지 못하게 보정한다(EnemyBlockLogic 참고). 두 콜라이더가 살짝 겹친 채로 멈춰서
        /// 접촉 데미지는 계속 들어오고, 플레이어가 밀고 들어오면 다음 프레임에 적이 경계 밖으로 밀려난다.
        /// </summary>
        private Vector2 BlockAgainstPlayer(Vector2 next)
        {
            if (_ownCollider == null || _targetCollider == null) return next;

            float minDistance = _ownCollider.bounds.extents.x + _targetCollider.bounds.extents.x - contactOverlap;
            Vector2 playerCenter = _targetCollider.bounds.center;

            return EnemyBlockLogic.Resolve(next, playerCenter, minDistance, _rigidbody.position - playerCenter);
        }

        private void UpdateSeparation()
        {
            _separationTimer -= Time.fixedDeltaTime;
            if (_separationTimer > 0f) return;

            _separationTimer = separationInterval;

            int count = Physics2D.OverlapCircleNonAlloc(
                _rigidbody.position, separationRadius, NeighborBuffer, enemyLayer);

            _neighborPositions.Clear();
            for (int i = 0; i < count; i++)
            {
                Collider2D other = NeighborBuffer[i];
                if (other == null) continue;
                // 자기 자신은 제외한다.
                if (other.attachedRigidbody == _rigidbody) continue;

                _neighborPositions.Add(other.transform.position);
            }

            _separation = SeparationLogic.ComputeSeparation(
                _rigidbody.position, _neighborPositions, separationRadius);
        }
    }
}
