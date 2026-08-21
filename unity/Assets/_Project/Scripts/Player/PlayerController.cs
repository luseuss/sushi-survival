using UnityEngine;
using UnityEngine.InputSystem;
using SushiSurvival.Core;

namespace SushiSurvival.Player
{
    [RequireComponent(typeof(FacingController))]
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private InputActionReference moveAction;

        private FacingController _facing;
        private CharacterAnimator _animator;
        private PlayerStats _stats;
        private Rigidbody2D _rigidbody;
        private Vector2 _moveInput;

        private void Awake()
        {
            _facing = GetComponent<FacingController>();
            _animator = GetComponent<CharacterAnimator>();
            _stats = GetComponent<PlayerStats>();
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void OnEnable() => moveAction.action.Enable();
        private void OnDisable() => moveAction.action.Disable();

        private void Update()
        {
            _moveInput = moveAction.action.ReadValue<Vector2>();
            _facing.UpdateFacing(_moveInput);

            // 간장새우처럼 몸통 애니메이션 아트가 없는 캐릭터는 이 컴포넌트가 없다.
            if (_animator != null)
                _animator.SetMoving(FacingLogic.IsMoving(_moveInput));
        }

        private void FixedUpdate()
        {
            float moveSpeed = _stats.GetValue(StatType.MoveSpeed);
            Vector2 velocity = _moveInput.normalized * moveSpeed;
            _rigidbody.MovePosition(_rigidbody.position + velocity * Time.fixedDeltaTime);
        }
    }
}
