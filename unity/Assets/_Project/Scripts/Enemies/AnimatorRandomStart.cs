using UnityEngine;

namespace SushiSurvival.Enemies
{
    /// <summary>
    /// 켜질 때마다 애니메이션을 무작위 지점에서 시작한다. 몹이 한꺼번에 나오면 걷기 박자가 똑같아 줄맞춰 뛰는 것처럼
    /// 보이는데, 이걸 붙이면 몹마다 박자가 어긋난다. 풀에서 다시 나올 때마다 새로 정해진다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AnimatorRandomStart : MonoBehaviour
    {
        [Tooltip("애니메이터 컨트롤러의 기본 상태 이름.")]
        [SerializeField] private string stateName = "Mob_Walk";

        private Animator _animator;
        private int _stateHash;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _stateHash = Animator.StringToHash(stateName);
        }

        private void OnEnable() => _animator.Play(_stateHash, 0, Random.value);
    }
}
