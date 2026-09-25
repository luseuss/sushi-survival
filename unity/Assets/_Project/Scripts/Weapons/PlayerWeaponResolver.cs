using System.Linq;
using UnityEngine;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 플레이어 오브젝트에 WeaponBase 파생 컴포넌트가 여러 개(예: 계란 양산 +
    /// 회전 우산) 있을 수 있어서 GetComponent&lt;WeaponBase&gt;()만으로는 어느 쪽이
    /// 잡힐지 불명확하다. 항상 정확히 하나만 enabled라는 전제 하에 그것만 고른다.
    /// </summary>
    public static class PlayerWeaponResolver
    {
        public static WeaponBase GetActive(GameObject player)
            => player == null ? null : player.GetComponents<WeaponBase>().FirstOrDefault(w => w.enabled);
    }
}
