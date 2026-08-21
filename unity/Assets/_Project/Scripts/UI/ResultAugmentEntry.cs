using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 결과 화면의 증강 항목 하나 — 아이콘과 "x3" 개수 표시.
    /// </summary>
    public class ResultAugmentEntry : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text countText;

        public void Bind(AugmentCount entry)
        {
            if (iconImage != null)
            {
                iconImage.sprite = entry.Data != null ? entry.Data.icon : null;
                iconImage.enabled = iconImage.sprite != null;
            }

            if (countText != null)
                countText.text = $"x{entry.Count}";
        }
    }
}
