using UnityEngine;
using UnityEngine.UI;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 화면 최상단 풀와이드 XP 게이지. 레벨업 팝업이 열려 스탯이 재계산되는
    /// 동안에도 부드럽게 채워지도록 스무딩한다.
    /// </summary>
    public class XpGaugeDisplay : MonoBehaviour
    {
        [SerializeField] private Core.LevelSystem levelSystem;
        [Tooltip("Image Type을 Filled로 설정한 채움 이미지.")]
        [SerializeField] private Image fillImage;
        [Tooltip("초당 채움 변화량.")]
        [SerializeField] private float fillSpeed = 3f;

        private float _currentFill;

        private void Update()
        {
            if (levelSystem == null || fillImage == null) return;

            _currentFill = HealthBarLogic.MoveTowardsFill(_currentFill, levelSystem.ProgressRatio, fillSpeed * Time.deltaTime);
            fillImage.fillAmount = _currentFill;
        }
    }
}
