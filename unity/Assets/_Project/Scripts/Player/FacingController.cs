using UnityEngine;

namespace SushiSurvival.Player
{
    /// <summary>
    /// FacingLogic을 감싸는 컴포넌트. 계란은 spriteRenderer.flipX만 사용하고,
    /// 간장새우/이나리는 이후 CurrentFacing 벡터를 공격 방향으로 그대로 쓴다.
    /// </summary>
    public class FacingController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public Vector2 CurrentFacing { get; private set; } = Vector2.down;

        public void UpdateFacing(Vector2 moveInput)
        {
            CurrentFacing = FacingLogic.ComputeFacing(CurrentFacing, moveInput);

            if (spriteRenderer != null)
                spriteRenderer.flipX = !FacingLogic.IsFacingRight(CurrentFacing);
        }
    }
}
