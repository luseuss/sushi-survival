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

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _enemy = GetComponent<EnemyBase>();
        }

        private void OnEnable()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            _target = playerObj != null ? playerObj.transform : null;

            // 풀에서 재사용되므로 이전 판의 상태를 지운다.
            _separation = Vector2.zero;
            _separationTimer = 0f;
            MoveScale = 1f;
        }

        private void FixedUpdate()
        {
            if (_target == null) return;

            UpdateSeparation();

            Vector2 chase = ((Vector2)_target.position - _rigidbody.position).normalized
                            * (monsterData.moveSpeed * MoveScale);
            Vector2 separation = _separation * (monsterData.moveSpeed * separationStrength);
            Vector2 knockback = _enemy != null ? _enemy.KnockbackVelocity : Vector2.zero;

            Vector2 move = (chase + separation + knockback) * Time.fixedDeltaTime;
            _rigidbody.MovePosition(_rigidbody.position + move);
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
