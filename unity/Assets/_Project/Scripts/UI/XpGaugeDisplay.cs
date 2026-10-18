using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Pickups;

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

        [Header("획득 연출")]
        [Tooltip("경험치가 오를 때 잠깐 섞이는 밝은 색.")]
        [SerializeField] private Color gainFlashColor = new Color(1f, 1f, 1f, 1f);
        [Tooltip("레벨이 오를 때 섞이는 색.")]
        [SerializeField] private Color levelUpFlashColor = new Color(1f, 0.85f, 0.3f, 1f);
        [Tooltip("반짝임이 사라지는 데 걸리는 실시간(초).")]
        [SerializeField] private float pulseSeconds = 0.25f;
        [Tooltip("반짝임 최대 세기일 때 원래 색에서 flash 색으로 섞이는 비율.")]
        [Range(0f, 1f)]
        [SerializeField] private float maxFlashMix = 0.6f;

        private float _currentFill;
        private Color _baseColor = Color.white;
        private bool _hasBaseColor;
        private float _pulse;
        private Color _pulseColor;
        private int _lastLevel = -1;
        private float _lastRatio;

        private void Update()
        {
            if (levelSystem == null || fillImage == null) return;

            _currentFill = HealthBarLogic.MoveTowardsFill(_currentFill, levelSystem.ProgressRatio, fillSpeed * Time.deltaTime);
            fillImage.fillAmount = _currentFill;

            UpdatePulse();
        }

        // 경험치가 오르면 게이지가 잠깐 밝아지고, 레벨이 오르면 더 세게 금빛으로 번쩍인다. 팝업 중(timeScale 0)에도 사라지도록 실시간으로 줄인다.
        private void UpdatePulse()
        {
            if (!_hasBaseColor)
            {
                _baseColor = fillImage.color;
                _hasBaseColor = true;
            }

            int level = levelSystem.CurrentLevel;
            float ratio = levelSystem.ProgressRatio;

            if (_lastLevel >= 0)
            {
                float strength = XPFeedbackLogic.GaugePulse(_lastLevel, _lastRatio, level, ratio);
                if (strength > 0f && strength >= _pulse)
                {
                    _pulse = strength;
                    _pulseColor = level > _lastLevel ? levelUpFlashColor : gainFlashColor;
                }
            }

            _lastLevel = level;
            _lastRatio = ratio;

            _pulse = XPFeedbackLogic.PulseDecay(_pulse, Time.unscaledDeltaTime, pulseSeconds);

            Color mixed = Color.Lerp(_baseColor, _pulseColor, _pulse * maxFlashMix);
            mixed.a = _baseColor.a;
            fillImage.color = _pulse > 0f ? mixed : _baseColor;
        }
    }
}
