using System;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Pickups;
using SushiSurvival.Player;

namespace SushiSurvival.Enemies
{
    [RequireComponent(typeof(Collider2D))]
    public class EnemyBase : MonoBehaviour
    {
        private const float ContactDamageInterval = 0.5f;

        [SerializeField] private MonsterData monsterData;
        [Tooltip("피격 시 밀려나는 초기 속도.")]
        [SerializeField] private float knockbackForce = 3f;
        [Tooltip("넉백 속도가 초당 이만큼씩 줄어든다.")]
        [SerializeField] private float knockbackDecay = 12f;

        private XPGemPoolSet _xpGemPools;
        private GameObjectPool _selfPool;

        public float CurrentHealth { get; private set; }

        /// <summary>EnemyAI가 이동에 더한다. 여기서 직접 위치를 옮기지 않는다.</summary>
        public Vector2 KnockbackVelocity { get; private set; }
        public event Action<EnemyBase> OnDeath;

        private float _contactTimer;

        private void Awake()
        {
            // GameObjectPool.CreateInstance가 Instantiate(prefab, transform)으로
            // 생성하므로, 부모를 타고 올라가면 항상 자기 풀을 찾을 수 있다.
            _selfPool = GetComponentInParent<GameObjectPool>();
        }

        private void OnEnable()
        {
            // 스폰되는 순간의 배율로 체력이 정해지고, 살아 있는 동안에는 바뀌지
            // 않는다. 이미 나와 있는 적이 시간이 지났다고 갑자기 단단해지면
            // 때리던 사람 입장에서 이유를 알 수 없다.
            CurrentHealth = monsterData.maxHealth * GetHealthScale();
            _contactTimer = 0f;
            KnockbackVelocity = Vector2.zero;
        }

        private float GetHealthScale()
        {
            if (!monsterData.scalesWithTime) return 1f;

            return GameManager.Instance != null ? GameManager.Instance.EnemyHealthMultiplier : 1f;
        }

        private void Update()
        {
            _contactTimer -= Time.deltaTime;
            KnockbackVelocity = KnockbackLogic.Decay(KnockbackVelocity, knockbackDecay, Time.deltaTime);
        }

        /// <summary>
        /// EnemySpawner가 Get() 직후 매번 호출해서 주입한다. 프리팹 에셋은
        /// 씬에만 존재하는 XPGemPool을 Inspector로 직접 참조할 수 없기 때문.
        /// </summary>
        public void SetXpGemPools(XPGemPoolSet pools) => _xpGemPools = pools;

        public void TakeDamage(float damage, Vector2 sourcePosition)
        {
            if (HealthLogic.IsDead(CurrentHealth)) return;

            KnockbackVelocity += KnockbackLogic.ComputeImpulse(
                sourcePosition, transform.position, knockbackForce, monsterData.knockbackResistance);

            CurrentHealth = HealthLogic.ApplyDamage(CurrentHealth, damage);

            if (HealthLogic.IsDead(CurrentHealth))
                Die();
        }

        private void Die()
        {
            if (_xpGemPools == null)
            {
                Debug.LogError($"{name}: xpGemPools가 설정되지 않아 XP 젬을 드롭할 수 없습니다.");
            }
            else
            {
                // 후반의 잡몹은 체력이 몇 배로 불어나 있으므로 보상도 올라간다.
                float elapsed = GameManager.Instance != null ? GameManager.Instance.ElapsedTime : 0f;
                XPGemType gemType = GemUpgradeLogic.Resolve(
                    monsterData.xpGemDrop, monsterData.upgradedGemDrop, monsterData.gemUpgradeTime, elapsed);

                GameObjectPool gemPool = _xpGemPools.GetPool(gemType);
                if (gemPool == null)
                    Debug.LogError($"{name}: {gemType} 등급 젬 풀이 비어 있습니다.");
                else
                    gemPool.Get(transform.position, Quaternion.identity);
            }

            if (GameManager.Instance != null)
                GameManager.Instance.RegisterKill();

            OnDeath?.Invoke(this);

            // 죽은 적을 풀로 돌려보낸다. 이걸 하지 않으면 시체가 화면에 남아
            // 계속 플레이어를 쫓아오고 접촉 데미지까지 준다.
            if (_selfPool != null)
                _selfPool.Release(gameObject);
            else
                Destroy(gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (_contactTimer > 0f) return;
            if (!collision.collider.TryGetComponent<PlayerHealth>(out var player)) return;

            player.TakeDamage(monsterData.contactDamage);
            _contactTimer = ContactDamageInterval;
        }
    }
}
