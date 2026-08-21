using SushiSurvival.Data;

namespace SushiSurvival.Enemies
{
    /// <summary>
    /// 시간이 지나면 같은 몹이 더 좋은 젬을 떨어뜨리게 한다. 후반의 잡몹은
    /// 체력이 몇 배로 불어나 있으므로, 보상이 그대로면 잡을 값어치가 사라진다.
    /// </summary>
    public static class GemUpgradeLogic
    {
        public static XPGemType Resolve(XPGemType baseType, XPGemType upgradedType,
                                        float upgradeTime, float elapsedSeconds)
        {
            // 0 이하는 "승급 없음"이다. 대부분의 몹이 이 상태다.
            if (upgradeTime <= 0f) return baseType;

            return elapsedSeconds >= upgradeTime ? upgradedType : baseType;
        }
    }
}
