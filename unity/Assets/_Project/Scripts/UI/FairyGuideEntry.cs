using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Companions;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>설명창 펫 목록의 한 줄 — 펫 그림, 이름, 역할, Lv1 수치.</summary>
    public class FairyGuideEntry : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text roleText;
        [SerializeField] private Text statsText;

        private Sprite[] _frames;
        private float _framesPerSecond;
        private float _playTime;

        private void OnEnable() => _playTime = 0f;

        // 설명창이 열려 있을 때만 켜져 있으므로, 닫히면 자동으로 멈춘다. 일시정지 중에도 돌도록 unscaled를 쓴다.
        private void Update()
        {
            if (iconImage == null || _frames == null || _frames.Length < 2) return;

            _playTime += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            int index = GuideAnimLogic.FrameIndex(_playTime, _framesPerSecond, _frames.Length);
            if (index >= 0 && _frames[index] != null) iconImage.sprite = _frames[index];
        }

        public void Bind(FairyData data)
        {
            if (data == null) return;

            if (iconImage != null)
            {
                Sprite icon = data.frames != null && data.frames.Length > 0 ? data.frames[0] : null;
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            _frames = data.frames;
            _framesPerSecond = data.framesPerSecond;

            if (nameText != null)
                nameText.text = data.displayName;

            if (roleText != null)
                roleText.text = data.roleLine;

            if (statsText != null && data.levels != null && data.levels.Length > 0)
                statsText.text = FairyDescriptionLogic.DescribeStats(data.levels[0]);
        }
    }
}
