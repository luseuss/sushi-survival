using UnityEngine;
using SushiSurvival.Companions;

namespace SushiSurvival.Core
{
    /// <summary>와사비 성공 뒤 레벨업 카드로 뜨는 "요정 소환 / 요정 N 강화" 선택지.</summary>
    public class FairyOption : IUpgradeOption
    {
        private readonly FairyController _controller;
        private readonly FairyChoice _choice;

        public Sprite Icon { get; }

        public string DisplayName => _choice.Kind == FairyChoiceKind.Summon
            ? "요정 소환"
            : $"요정 {_choice.Index + 1} 강화 Lv{_controller.Levels[_choice.Index] + 1}";

        public string Description => _choice.Kind == FairyChoiceKind.Summon
            ? $"곁을 따라다니며 가까운 적을 공격한다 ({_controller.Count + 1}/{_controller.MaxCount})"
            : _controller.DescribeUpgrade(_choice.Index);

        public FairyOption(FairyController controller, FairyChoice choice, Sprite icon)
        {
            _controller = controller;
            _choice = choice;
            Icon = icon;
        }

        public void Apply()
        {
            if (_choice.Kind == FairyChoiceKind.Summon)
                _controller.Summon();
            else
                _controller.Upgrade(_choice.Index);
        }
    }
}
