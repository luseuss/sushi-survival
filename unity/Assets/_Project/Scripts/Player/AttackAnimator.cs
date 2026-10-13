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
        private static readonly int ShortAttackHash = Animator.StringToHash("ShortAttack");

        private Animator _animator;
        private bool _hasShortAttackParameter;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            // 짧은 모션이 없는 컨트롤러(간장새우·이나리)에 값을 쓰면 매번 경고가 찍히므로 있는지 먼저 확인한다.
            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.nameHash == ShortAttackHash)
                {
                    _hasShortAttackParameter = true;
                    break;
                }
            }
        }

        /// <param name="shortMotion">
        /// true면 모션을 반만 재생한다(컨트롤러에 ShortAttack 파라미터와 짧은 모션 상태가 있을 때). 한 번만 때리는
        /// 레벨에서 모션도 한 번만 휘두르게 하려는 용도다.
        /// </param>
        public void TriggerAttack(bool shortMotion = false)
        {
            if (_hasShortAttackParameter)
                _animator.SetBool(ShortAttackHash, shortMotion);

            _animator.SetTrigger(AttackHash);
        }
    }
}
