using UnityEngine;
using UnityEngine.InputSystem;

namespace SushiSurvival.Player
{
    /// <summary>
    /// FacingLogic을 감싸는 컴포넌트. 계란은 spriteRenderer.flipX만 사용하고,
    /// 간장새우/이나리는 이후 CurrentFacing 벡터를 공격 방향으로 그대로 쓴다.
    ///
    /// 마우스가 있으면 마우스 쪽을 바라본다 — 무기와 공격이 모두 이 방향을 따라간다. 마우스가 없거나(패드 등)
    /// 시간이 멈춘 동안에는 이동 방향을 따른다.
    /// </summary>
    public class FacingController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("켜면 마우스가 있는 동안 마우스 방향으로 공격하고 바라본다. 끄면 이동 방향을 따른다.")]
        [SerializeField] private bool aimWithMouse = true;
        [Tooltip("마우스가 플레이어에서 이 거리(월드 단위) 안이면 방향을 바꾸지 않는다. 바로 위에서 방향이 튀는 걸 막는다.")]
        [SerializeField] private float mouseDeadZone = 0.15f;

        private Camera _camera;

        public Vector2 CurrentFacing { get; private set; } = Vector2.down;

        public void UpdateFacing(Vector2 moveInput)
        {
            if (!TryAimAtMouse())
                CurrentFacing = FacingLogic.ComputeFacing(CurrentFacing, moveInput);

            if (spriteRenderer != null)
                spriteRenderer.flipX = !FacingLogic.IsFacingRight(CurrentFacing);
        }

        // 마우스로 방향을 정했으면 true. 마우스가 없거나 카메라를 못 찾거나 시간이 멈춰 있으면 false.
        private bool TryAimAtMouse()
        {
            if (!aimWithMouse) return false;

            // 일시정지·팝업 중에 마우스를 움직여 캐릭터가 돌아서는 걸 막는다. 방향은 그대로 둔다.
            if (Time.timeScale <= 0f) return true;

            Mouse mouse = Mouse.current;
            if (mouse == null) return false;

            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return false;

            Vector2 screen = mouse.position.ReadValue();

            // 정사영 카메라라 카메라 높이만큼 떨어진 평면(플레이어가 있는 z=0)으로 변환한다.
            Vector3 world = _camera.ScreenToWorldPoint(
                new Vector3(screen.x, screen.y, Mathf.Abs(_camera.transform.position.z)));

            CurrentFacing = FacingLogic.ComputeMouseFacing(CurrentFacing, transform.position, world, mouseDeadZone);
            return true;
        }
    }
}
