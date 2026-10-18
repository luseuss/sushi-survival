using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>왕궁 와사비 가위바위보에서 이겼을 때 받는 보상의 종류.</summary>
    public enum WasabiReward
    {
        /// <summary>이미 변신한 무기라 더 받을 것이 없다. 와사비 버튼도 숨긴다.</summary>
        None,
        /// <summary>무기가 변신한다(아델린 양산→회전 우산, 카마리온 소총→샷건).</summary>
        WeaponTransform,
        /// <summary>변신할 무기가 없는 캐릭터의 대체 보상 — 스탯 버프.</summary>
        StatBuff,
    }

    /// <summary>와사비 보상이 무엇인지 정하는 규칙과, 와사비 변신 무기의 추가 공격력 문구.</summary>
    public static class WasabiRewardLogic
    {
        public static WasabiReward Decide(bool canTransform, bool alreadyTransformed)
        {
            if (alreadyTransformed) return WasabiReward.None;
            return canTransform ? WasabiReward.WeaponTransform : WasabiReward.StatBuff;
        }

        /// <summary>레벨업 팝업에 와사비 버튼을 보여줄지.</summary>
        public static bool ShowsButton(WasabiReward reward) => reward != WasabiReward.None;

        /// <summary>1.25 → 25. 1 이하면 0.</summary>
        public static int BonusPercent(float multiplier) => Mathf.Max(0, Mathf.RoundToInt((multiplier - 1f) * 100f));
    }
}
