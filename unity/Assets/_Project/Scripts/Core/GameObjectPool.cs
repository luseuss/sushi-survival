using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// ObjectPool&lt;GameObject&gt;를 감싸는 MonoBehaviour 래퍼.
    /// 몹/총알/젬처럼 동시 개체 수가 많은 프리팹에 사용한다.
    /// </summary>
    public class GameObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int prewarmCount = 20;

        private ObjectPool<GameObject> _pool;

        private void Awake()
        {
            WarnAboutSiblingPools();

            _pool = new ObjectPool<GameObject>(
                factory: CreateInstance,
                onGet: go => go.SetActive(true),
                onRelease: go => go.SetActive(false));

            for (int i = 0; i < prewarmCount; i++)
                _pool.Release(_pool.Get());
        }

        /// <summary>
        /// 풀링된 오브젝트는 GetComponentInParent&lt;GameObjectPool&gt;()로 자기 풀을
        /// 찾는다. 한 오브젝트에 풀이 둘 이상 붙어 있으면 전부 첫 번째 풀을
        /// 자기 풀로 착각해서, 시간이 지날수록 풀 안에 엉뚱한 프리팹이 쌓인다.
        /// 실제로 캘리포니아롤이 BasicMob 풀로 반환되어 잡몹 A가 사라지는
        /// 버그를 겪었다. 조용히 깨지는 종류라 시작할 때 크게 알린다.
        /// </summary>
        private void WarnAboutSiblingPools()
        {
            int count = GetComponents<GameObjectPool>().Length;
            if (count <= 1) return;

            Debug.LogError($"{name}: 한 오브젝트에 GameObjectPool이 {count}개 붙어 있습니다. " +
                           "반환된 오브젝트가 전부 첫 번째 풀로 들어가 프리팹이 섞입니다. " +
                           "풀마다 GameObject를 따로 만드세요.");
        }

        private GameObject CreateInstance()
        {
            var go = Instantiate(prefab, transform);
            go.SetActive(false);
            return go;
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            var go = _pool.Get();
            go.transform.SetPositionAndRotation(position, rotation);
            return go;
        }

        public void Release(GameObject go) => _pool.Release(go);
    }
}
