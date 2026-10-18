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
        private static readonly Collider2D[] MobBlockBuffer = new Collider2D[16];

        [SerializeField] private MonsterData monsterData;
        [Tooltip("다른 적을 찾을 레이어. Enemy 레이어를 지정한다.")]
        [SerializeField] private LayerMask enemyLayer;
        [Tooltip("이 반경 안의 다른 적에게서 밀려난다.")]
        [SerializeField] private float separationRadius = 0.6f;
        [Tooltip("이동속도 대비 분리 힘의 배율. 몹을 서로 멀리 떼어 놓는 힘이다. 0이면 이 힘을 쓰지 않는다 — " +
                 "몹끼리 겹치지 않는 건 아래 '몹끼리 막기'가 맡는다.")]
        [SerializeField] private float separationStrength = 1.5f;
        [Tooltip("분리 벡터 갱신 주기(초). 매 프레임 물리 쿼리를 돌리지 않기 위함.")]
        [SerializeField] private float separationInterval = 0.1f;

        [Header("몹끼리 막기")]
        [Tooltip("켜면 다른 몹의 몸 안으로 들어가지 못하고 그 경계를 따라 미끄러진다. 서로 밀어내지는 않는다. " +
                 "보스처럼 몹을 뚫고 돌진해야 하는 몹은 끈다.")]
        [SerializeField] private bool blockAgainstMobs = true;
        [Tooltip("몸이 이미 겹쳐 있을 때 초당 이만큼씩만 천천히 벌어진다(월드 단위). 클수록 빨리 떨어진다.")]
        [SerializeField] private float mobOverlapResolveSpeed = 0.6f;
        [Tooltip("몹끼리 막을 때 이웃을 찾는 반경(월드 단위). 가장 큰 몹과도 부딪힐 수 있게 넉넉히 잡는다.")]
        [SerializeField] private float mobBlockQueryRadius = 1.2f;

        private readonly List<Vector2> _neighborPositions = new List<Vector2>(MaxNeighbors);

        [Tooltip("플레이어와 겹치는 정도(월드 단위). 살짝 겹쳐 있어야 접촉 데미지가 계속 들어온다.")]
        [SerializeField] private float contactOverlap = 0.02f;

        // 플레이어 콜라이더 중심은 물리 스텝마다 한 번만 읽어 모든 적이 나눠 쓴다(적마다 bounds를 부르면 네이티브 호출이 수천 번이다).
        private static int _playerCenterStep = -1;
        private static Vector2 _playerCenter;

        private Collider2D _ownCollider;
        private Collider2D _targetCollider;
        // 콜라이더 반폭은 크기가 안 바뀌므로 켜질 때 한 번만 잰다.
        private float _ownHalfWidth;
        private float _targetHalfWidth;
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
            _ownHalfWidth = _ownCollider != null ? _ownCollider.bounds.extents.x : 0f;
            _targetHalfWidth = _targetCollider != null ? _targetCollider.bounds.extents.x : 0f;

            // 풀에서 재사용되므로 이전 판의 상태를 지운다.
            _separation = Vector2.zero;
            _separationTimer = 0f;
            MoveScale = 1f;
            LockedDirection = Vector2.zero;
        }

        private void FixedUpdate()
        {
            if (_target == null) return;

            // 분리 힘이 0이면 몹끼리 밀어내지 않는다 — 이웃을 찾는 물리 쿼리도 건너뛴다.
            if (separationStrength > 0f)
                UpdateSeparation();

            Vector2 chaseDirection = LockedDirection != Vector2.zero
                ? LockedDirection
                : ((Vector2)_target.position - _rigidbody.position).normalized;
            Vector2 chase = chaseDirection
                            * (monsterData.moveSpeed * MoveScale);
            Vector2 separation = _separation * (monsterData.moveSpeed * separationStrength);
            Vector2 knockback = _enemy != null ? _enemy.KnockbackVelocity : Vector2.zero;

            Vector2 move = (chase + separation + knockback) * Time.fixedDeltaTime;
            Vector2 next = BlockAgainstMobs(_rigidbody.position, _rigidbody.position + move);

            // 플레이어는 마지막에 막는다 — 접촉 데미지가 들어오려면 플레이어와의 간격이 이 규칙으로 정해져야 한다.
            _rigidbody.MovePosition(BlockAgainstPlayer(next));
        }

        /// <summary>
        /// 다른 몹의 몸 안으로 파고들지 못하게 보정한다(MobBlockLogic 참고). 이웃은 움직이지 않고 이 몹만 경계를 따라
        /// 미끄러지므로 서로 밀치는 느낌 없이 겹치지만 않는다. 보스의 돌진처럼 방향이 고정된 이동은 몹을 뚫고 가야 해서 건너뛴다.
        /// </summary>
        private Vector2 BlockAgainstMobs(Vector2 current, Vector2 next)
        {
            if (!blockAgainstMobs || _ownCollider == null || LockedDirection != Vector2.zero) return next;

            int count = Physics2D.OverlapCircleNonAlloc(current, mobBlockQueryRadius, MobBlockBuffer, enemyLayer);
            float ownHalfWidth = _ownHalfWidth;
            float resolveStep = mobOverlapResolveSpeed * Time.fixedDeltaTime;

            for (int i = 0; i < count; i++)
            {
                Collider2D other = MobBlockBuffer[i];
                if (other == null || other.attachedRigidbody == _rigidbody) continue;

                // bounds는 호출마다 네이티브 계산이라 한 번만 읽는다.
                Bounds otherBounds = other.bounds;
                float minDistance = ownHalfWidth + otherBounds.extents.x;
                next = MobBlockLogic.Resolve(current, next, otherBounds.center, minDistance, resolveStep);
            }

            return next;
        }

        /// <summary>
        /// 플레이어 몸 안으로 파고들지 못하게 보정한다(EnemyBlockLogic 참고). 두 콜라이더가 살짝 겹친 채로 멈춰서
        /// 접촉 데미지는 계속 들어오고, 플레이어가 밀고 들어오면 다음 프레임에 적이 경계 밖으로 밀려난다.
        /// </summary>
        private Vector2 BlockAgainstPlayer(Vector2 next)
        {
            if (_ownCollider == null || _targetCollider == null) return next;

            float minDistance = _ownHalfWidth + _targetHalfWidth - contactOverlap;
            Vector2 playerCenter = PlayerCenterThisStep();

            return EnemyBlockLogic.Resolve(next, playerCenter, minDistance, _rigidbody.position - playerCenter);
        }

        private Vector2 PlayerCenterThisStep()
        {
            int step = Time.frameCount; // FixedUpdate는 한 프레임에 여러 번 돌 수 있지만 플레이어는 그 안에서 거의 안 움직여 프레임 단위로 충분하다.
            if (_playerCenterStep != step)
            {
                _playerCenterStep = step;
                _playerCenter = _targetCollider.bounds.center;
            }

            return _playerCenter;
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
