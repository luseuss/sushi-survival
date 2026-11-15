using System.Linq;
using UnityEngine;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 플레이어 오브젝트에 WeaponBase 파생 컴포넌트가 여러 개(예: 계란 양산 +
    /// 회전 우산) 있을 수 있어서 GetComponent&lt;WeaponBase&gt;()만으로는 어느 쪽이
    /// 잡힐지 불명확하다. 켜진 무기가 하나면 그것을 고르고, 와사비 보상으로 양산과 우산이
    /// 함께 켜져 있으면 우산을 대표 무기로 고른다(레벨·추가 공격력·변신 여부가 우산 쪽에 쌓이므로).
    /// </summary>
    public static class PlayerWeaponResolver
    {
        public static WeaponBase GetActive(GameObject player)
        {
            if (player == null) return null;

            var enabledWeapons = player.GetComponents<WeaponBase>().Where(w => w.enabled).ToList();

            WeaponBase umbrella = enabledWeapons.OfType<RotatingUmbrellaWeapon>().FirstOrDefault();
            return umbrella != null ? umbrella : enabledWeapons.FirstOrDefault();
        }
    }
}