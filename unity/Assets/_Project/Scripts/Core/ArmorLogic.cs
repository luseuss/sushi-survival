using UnityEngine;

namespace SushiSurvival.Core
{
    public static class ArmorLogic
    {
        /// <summary>
        /// 기획서 하드캡. 이 값을 넘기면 피해 감소 100%(무적) 조합이 생긴다.
        /// StatSystem에서도 캡을 걸지만, 설정 실수로 무적이 되는 일이 없도록
        /// 실제 사용 지점에서 한 번 더 막는다.
        /// </summary>
        public const float MaxArmor = 0.5f;

        public static float ApplyArmor(float damage, float armor)
        {
            float safeArmor = Mathf.Clamp(armor, 0f, MaxArmor);
            return damage * (1f - safeArmor);
        }
    }
}
