using UnityEngine;
using SushiSurvival.Companions;

namespace SushiSurvival.Core
{
    /// <summary>와사비 성공 뒤 레벨업 카드로 뜨는 "{펫} 소환 / {펫} 강화" 선택지.</summary>
    public class FairyOption : IUpgradeOption
    {
        private readonly FairyController _controller;
        private readonly FairyChoice _choice;

        public Sprite Icon { get; }

        public string DisplayName => _choice.Kind == FairyChoiceKind.Summon
            ? $"{_controller.NameOfKind(_choice.KindIndex)} 소환"
            : $"{_controller.NameOfFairy(_choice.Index)} 강화 Lv{_controller.Levels[_choice.Index] + 1}";

        public string Description => _choice.Kind == FairyChoiceKind.Summon
            ? _controller.DescribeSummon(_choice.KindIndex)
            : _controller.DescribeUpgrade(_choice.Index);

        public FairyOption(FairyController controller, FairyChoice choice, Sprite fallbackIcon)
        {
            _controller = controller;
            _choice = choice;

            // 소환 카드는 그 펫의 그림을, 강화 카드는 강화할 요정(가진 펫)의 그림을 아이콘으로 쓴다.
            int kind = choice.Kind == FairyChoiceKind.Summon
                ? choice.KindIndex
                : (choice.Index >= 0 && choice.Index < controller.Kinds.Count ? controller.Kinds[choice.Index] : -1);
            Sprite petIcon = kind >= 0 ? controller.IconOfKind(kind) : null;
            Icon = petIcon != null ? petIcon : fallbackIcon;
        }

        public void Apply()
        {
            if (_choice.Kind == FairyChoiceKind.Summon)
                _controller.Summon(_choice.KindIndex);
            else
                _controller.Upgrade(_choice.Index);
        }
    }
}
