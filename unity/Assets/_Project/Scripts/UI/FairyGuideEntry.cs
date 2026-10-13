using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Companions;
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

        public void Bind(FairyData data)
        {
            if (data == null) return;

            if (iconImage != null)
            {
                Sprite icon = data.frames != null && data.frames.Length > 0 ? data.frames[0] : null;
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (nameText != null)
                nameText.text = data.displayName;

            if (roleText != null)
                roleText.text = data.roleLine;

            if (statsText != null && data.levels != null && data.levels.Length > 0)
                statsText.text = FairyDescriptionLogic.DescribeStats(data.levels[0]);
        }
    }
}
