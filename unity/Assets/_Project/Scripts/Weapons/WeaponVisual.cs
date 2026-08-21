using UnityEngine;
using SushiSurvival.Player;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 캐릭터 옆에 붙어 다니는 무기 그림(양산, 라이플 등)의 위치와 방향을 맡는다.
    /// 캐릭터 본체 스프라이트와 분리돼 있어야 공격 중에도 캐릭터가 그대로 보인다.
    /// </summary>
    public class WeaponVisual : MonoBehaviour
    {
        [Tooltip("FlipOnly는 좌우 반전만(계란 양산), RotateToFacing은 시선 방향 회전(간장새우 라이플).")]
        [SerializeField] private WeaponOrientMode orientMode = WeaponOrientMode.FlipOnly;
        [Tooltip("FlipOnly에서 쓰는 캐릭터 기준 무기 위치. 오른쪽을 볼 때 기준.")]
        [SerializeField] private Vector2 baseOffset = new Vector2(0.5f, 0f);
        [Tooltip("RotateToFacing에서 쓰는 캐릭터로부터의 거리.")]
        [SerializeField] private float orbitDistance = 0.5f;
        [Tooltip("스프라이트가 그려진 기본 방향 보정(도). 그림이 오른쪽을 향하면 0, " +
                 "왼쪽을 향하면 180. 라이플 아트는 총구가 왼쪽이라 180이다.")]
        [SerializeField] private float spriteAngleOffset;
        [SerializeField] private FacingController facing;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private void LateUpdate()
        {
            if (facing == null) return;

            Vector2 currentFacing = facing.CurrentFacing;

            if (orientMode == WeaponOrientMode.RotateToFacing)
            {
                float rotation = WeaponVisualLogic.ComputeRotationDegrees(currentFacing) + spriteAngleOffset;

                transform.localPosition = WeaponVisualLogic.ComputeOrbitOffset(currentFacing, orbitDistance);
                transform.localRotation = Quaternion.Euler(0f, 0f, rotation);

                // 회전 결과 그림이 거꾸로 서는 각도에서는 세로로 뒤집어 준다.
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = false;
                    spriteRenderer.flipY = WeaponVisualLogic.ShouldFlipVertically(rotation);
                }
                return;
            }

            bool facingRight = FacingLogic.IsFacingRight(currentFacing);
            transform.localPosition = WeaponVisualLogic.ComputeLocalOffset(baseOffset, facingRight);
            transform.localRotation = Quaternion.identity;

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !facingRight;
                spriteRenderer.flipY = false;
            }
        }
    }
}
