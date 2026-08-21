using UnityEngine;

namespace SushiSurvival.Player
{
    /// <summary>
    /// 캐릭터 몸통의 이동 애니메이션을 담당한다. 캐릭터 3종 모두 Idle/Move
    /// 2상태 + IsMoving bool 규약을 쓰므로, 캐릭터를 추가할 때는 새 스크립트
    /// 없이 애니메이터만 그 규약대로 만들어 붙이면 된다.
    ///
    /// 공격 모션은 여기 넣지 않는다 — 계란(양산)·간장새우(라이플)는 무기가
    /// 캐릭터와 별개의 스프라이트라서, 몸통 애니메이터에 공격 상태를 넣으면
    /// 캐릭터가 무기 그림으로 교체돼 버린다. 공격은 <see cref="AttackAnimator"/> 담당.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimator : MonoBehaviour
    {
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        private Animator _animator;

        private void Awake() => _animator = GetComponent<Animator>();

        public void SetMoving(bool isMoving) => _animator.SetBool(IsMovingHash, isMoving);
    }
}
