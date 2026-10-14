using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>증강 설명창의 한 줄 — 아이콘, 이름, 한 번에 오르는 효과, 최대 선택 횟수.</summary>
    public class AugmentGuideEntry : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text effectText;
        [SerializeField] private Text limitText;

        /// <summary>아이콘 배율을 바꾼다(설명창 탭 연출용). 1이면 원래 크기.</summary>
        public void SetIconScale(float scale)
        {
            if (iconImage != null) iconImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        public void Bind(AugmentData data)
        {
            if (data == null) return;

            if (iconImage != null)
            {
                iconImage.sprite = data.icon;
                iconImage.enabled = data.icon != null;
            }

            if (nameText != null)
                nameText.text = data.augmentName;

            // 레벨업 카드와 같은 규칙 — 직접 적은 설명이 있으면 그것을, 없으면 스탯과 값으로 만든다.
            if (effectText != null)
                effectText.text = string.IsNullOrEmpty(data.description)
                    ? UpgradeDescriptionLogic.DescribeAugment(data.statType, data.valuePerPick)
                    : data.description;

            if (limitText != null)
                limitText.text = UpgradeDescriptionLogic.DescribeAugmentLimit(
                    data.statType, data.valuePerPick, data.maxCap);
        }
    }
}
