using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Enemies;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 직진하는 투사체. 적중 수가 관통 수를 넘거나 수명이 다하면 풀로 돌아간다.
    /// 아무것도 맞히지 못해도 수명 타이머로 반드시 회수되므로 풀이 고갈되지 않는다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [Tooltip("기획서에 없는 값 — 플레이테스트로 조정한다.")]
        [SerializeField] private float speed = 10f;
        [Tooltip("아무것도 맞히지 못했을 때 회수되기까지의 시간(초).")]
        [SerializeField] private float lifetime = 3f;

        private Vector2 _direction;
        private float _damage;
        private int _pierceCount;
        private int _enemiesHit;
        private float _lifeTimer;
        private GameObjectPool _pool;

        public void Initialize(Vector2 direction, float damage, int pierceCount, GameObjectPool pool)
        {
            _direction = direction.normalized;
            _damage = damage;
            _pierceCount = pierceCount;
            _pool = pool;

            _enemiesHit = 0;
            _lifeTimer = lifetime;
        }

        private void Update()
        {
            transform.position += (Vector3)(_direction * speed * Time.deltaTime);

            _lifeTimer -= Time.deltaTime;
            if (_lifeTimer <= 0f)
                Despawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<EnemyBase>(out var enemy)) return;

            enemy.TakeDamage(_damage, transform.position);
            _enemiesHit++;

            if (ProjectileLogic.ShouldDespawn(_enemiesHit, _pierceCount))
                Despawn();
        }

        private void Despawn()
        {
            if (_pool != null)
                _pool.Release(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
