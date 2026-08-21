using UnityEngine;
using SushiSurvival.Weapons;

namespace SushiSurvival.Core
{
    public class WeaponLevelUpOption : IUpgradeOption
    {
        private readonly WeaponBase _weapon;

        public string DisplayName => $"{_weapon.WeaponName} 강화 Lv{_weapon.CurrentLevel + 1}";
        public Sprite Icon { get; }

        public WeaponLevelUpOption(WeaponBase weapon, Sprite icon)
        {
            _weapon = weapon;
            Icon = icon;
        }

        public void Apply() => _weapon.LevelUp();
    }
}
