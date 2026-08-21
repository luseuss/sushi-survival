using UnityEngine;

namespace SushiSurvival.Player
{
    /// <summary>
    /// 공격 모션을 재생하는 애니메이터 래퍼. 붙는 위치가 캐릭터마다 다르다:
    /// 계란·간장새우는 무기 오브젝트(WeaponVisual)에, 이나리는 무기 오브젝트가
    /// 없는 맨몸 공격이라 캐릭터 본체에 붙인다. 무기 스크립트는 어느 쪽이든
    /// 이 컴포넌트만 참조하므로 캐릭터별 분기가 필요 없다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AttackAnimator : MonoBehaviour
    {
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        private Animator _animator;

        private void Awake() => _animator = GetComponent<Animator>();

        public void TriggerAttack() => _animator.SetTrigger(AttackHash);
    }
}
