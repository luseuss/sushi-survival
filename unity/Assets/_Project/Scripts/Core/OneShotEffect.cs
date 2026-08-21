using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 애니메이션을 한 번 재생하고 스스로 풀로 돌아가는 이펙트. 반환 경로가
    /// 타이머 하나뿐이라 어떤 경우에도 풀이 고갈되지 않는다.
    /// </summary>
    public class OneShotEffect : MonoBehaviour
    {
        [Tooltip("애니메이션 클립 길이와 맞춘다. 짧으면 잘리고 길면 마지막 프레임이 남는다.")]
        [SerializeField] private float duration = 0.65f;

        public float Duration => duration;

        private GameObjectPool _pool;
        private float _timer;

        private void Awake()
        {
            // GameObjectPool.CreateInstance가 풀의 자식으로 생성하므로
            // 부모를 타고 올라가면 자기 풀을 찾을 수 있다.
            _pool = GetComponentInParent<GameObjectPool>();
        }

        private void OnEnable() => _timer = duration;

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            if (_pool != null)
                _pool.Release(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
