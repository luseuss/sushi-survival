using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 클릭으로 발동하는 스킬(아델린 회전 우산, 카마리온 와사비 샷건)이 같이 쓰는 입력 확인.
    /// 마우스 왼쪽 버튼을 이번 프레임에 눌렀는지, 단 레벨업·일시정지 창이 떠 있거나 화면 위 버튼을 누르는 중이면 무시한다.
    /// </summary>
    public static class SkillInput
    {
        public static bool ClickedThisFrame()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return false;

            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            return UmbrellaSkillLogic.ClickAccepted(Time.timeScale, overUi);
        }
    }
}
