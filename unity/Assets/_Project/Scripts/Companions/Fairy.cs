using System;
using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Enemies;
using SushiSurvival.Player;
using SushiSurvival.Weapons;

namespace SushiSurvival.Companions
{
    [Serializable]
    public struct FairyMotion
    {
        [Tooltip("플레이어 중심에서 슬롯까지 거리.")]
        public float radius;
        [Tooltip("요정 사이의 각도 간격(도). 머리 위(90°)를 중심으로 부채꼴로 선다.")]
        public float arcSpacingDegrees;
        [Tooltip("위아래로 둥둥 떠다니는 폭.")]
        public float bobAmplitude;
        public float bobSpeed;
        [Tooltip("클수록 슬롯을 빨리 따라간다.")]
        public float followSharpness;

        public static FairyMotion Default => new FairyMotion
        {
            radius = 1.2f,
            arcSpacingDegrees = 60f,
            bobAmplitude = 0.12f,
            bobSpeed = 3f,
            followSharpness = 6f
        };
    }

    /// <summary>
    /// 요정 한 마리. 플레이어 주변 슬롯을 부드럽게 따라다니고, 쿨타임마다 사거리 안의 가장 가까운
    /// 적에게 기존 Projectile을 쏜다. 수치는 WeaponData의 레벨 표에서 읽고 플레이어 증강 배율을 곱한다.
    /// </summary>
    public class Fairy : MonoBehaviour
    {
        [Tooltip("공격속도 증강이 아무리 쌓여도 이 값보다 짧아지지 않는다(무한 연사 방지).")]
        [SerializeField] private float minCooldown = 0.2f;

        private readonly WeaponCooldown _cooldown = new WeaponCooldown();
        private readonly List<Vector2> _positions = new List<Vector2>();
        private readonly List<Transform> _targets = new List<Transform>();

        private Transform _player;
        private PlayerStats _stats;
        private WeaponData _data;
        private GameObjectPool _pool;
        private LayerMask _enemyLayer;
        private FairyMotion _motion;
        private int _level = 1;
        private int _slotIndex;
        private int _slotCount = 1;
        private float _time;

        public int Level => _level;

        public void Initialize(Transform player, PlayerStats stats, WeaponData data, GameObjectPool projectilePool,
                               LayerMask enemyLayer, FairyMotion motion, int level, int slotIndex, int slotCount)
        {
            _player = player;
            _stats = stats;
            _data = data;
            _pool = projectilePool;
            _enemyLayer = enemyLayer;
            _motion = motion;
            _slotIndex = slotIndex;
            _slotCount = slotCount;
            SetLevel(level);

            // 소환되는 순간 플레이어 옆에서 시작하게 해 화면 구석에서 날아오지 않게 한다.
            if (_player != null)
                transform.position = TargetPosition();
        }

        public void SetPlayer(Transform player, PlayerStats stats)
        {
            _player = player;
            _stats = stats;
        }

        public void SetLevel(int level)
        {
            int max = _data != null && _data.levels != null ? _data.levels.Length : 1;
            _level = Mathf.Clamp(level, 1, Mathf.Max(1, max));
        }

        public void SetSlot(int slotIndex, int slotCount)
        {
            _slotIndex = slotIndex;
            _slotCount = slotCount;
        }

        private void Update()
        {
            if (_player == null || _data == null || _data.levels == null || _data.levels.Length == 0) return;

            _time += Time.deltaTime;
            transform.position = FairySlotLogic.Follow(
                transform.position, TargetPosition(), _motion.followSharpness, Time.deltaTime);

            _cooldown.Tick(Time.deltaTime);
            if (!_cooldown.IsReady) return;

            WeaponLevelStats stats = _data.levels[_level - 1];
            if (!TryFire(stats)) return;

            _cooldown.Reset(CooldownLogic.ApplyAttackSpeed(
                stats.cooldown, StatMultiplier(StatType.AttackSpeed), minCooldown));
        }

        private Vector2 TargetPosition()
            => (Vector2)_player.position + FairySlotLogic.SlotOffset(
                _slotIndex, _slotCount, _motion.radius, _motion.arcSpacingDegrees,
                _time, _motion.bobAmplitude, _motion.bobSpeed);

        /// <summary>타깃이 없으면 쏘지 않고 false — 쿨타임도 소모하지 않아 적이 나타나는 즉시 쏜다.</summary>
        private bool TryFire(WeaponLevelStats stats)
        {
            if (_pool == null)
            {
                Debug.LogError($"{name}: projectilePool이 없어 발사할 수 없습니다.");
                return false;
            }

            float range = stats.range * StatMultiplier(StatType.AttackRange);
            Vector2 origin = transform.position;

            _positions.Clear();
            _targets.Clear();

            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, _enemyLayer);
            foreach (Collider2D hit in hits)
            {
                if (!hit.TryGetComponent<EnemyBase>(out _)) continue;

                _positions.Add(hit.transform.position);
                _targets.Add(hit.transform);
            }

            int index = FairyTargetLogic.NearestIndex(origin, _positions, range);
            if (index < 0) return false;

            Vector2 direction = ((Vector2)_targets[index].position - origin).normalized;
            float rotation = WeaponVisualLogic.ComputeRotationDegrees(direction);

            GameObject projectileObj = _pool.Get(origin, Quaternion.Euler(0f, 0f, rotation));
            if (!projectileObj.TryGetComponent<Projectile>(out var projectile))
            {
                Debug.LogError($"{projectileObj.name}: Projectile 컴포넌트가 없어 발사할 수 없습니다.");
                _pool.Release(projectileObj);
                return false;
            }

            projectile.Initialize(direction, stats.damage * StatMultiplier(StatType.AttackDamage),
                                  stats.pierceCount, _pool);
            return true;
        }

        private float StatMultiplier(StatType stat) => _stats != null ? _stats.GetValue(stat) : 1f;
    }
}
