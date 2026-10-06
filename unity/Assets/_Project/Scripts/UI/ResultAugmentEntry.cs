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
            => Bind(entry.Data != null ? entry.Data.icon : null, entry.Count);

        /// <summary>AugmentData가 아닌 항목(와사비 등)도 같은 모양으로 보여줄 때 쓴다.</summary>
        public void Bind(Sprite icon, int count)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (countText != null)
                countText.text = $"x{count}";
        }
    }
}
