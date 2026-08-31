using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 풀링된 오브젝트는 SetActive(true)로 재활성화되는데, ParticleSystem의
    /// "Play On Awake"는 최초 생성 때 한 번만 불리는 Awake에만 반응한다.
    /// 그래서 두 번째 재사용부터 파티클이 안 나올 수 있다. OnEnable에서
    /// 명시적으로 재생해 이 문제를 피한다.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledParticlePlayer : MonoBehaviour
    {
        private ParticleSystem _particles;

        private void Awake() => _particles = GetComponent<ParticleSystem>();

        private void OnEnable()
        {
            // Clear를 먼저 하지 않으면 이전 위치에서 남은 파티클이 새 위치로
            // 순간이동한 것처럼 한 프레임 보인다.
            _particles.Clear();
            _particles.Play();
        }
    }
}
