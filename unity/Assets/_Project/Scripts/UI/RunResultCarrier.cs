using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    public static class RunResultCarrier
    {
        public static RunOutcome Outcome;
        public static float ElapsedTime;
        public static int Level;
        public static int KillCount;
        public static IReadOnlyList<AugmentCount> Augments;
        public static CharacterData SelectedCharacterData;

        // 👇 보스 씬 데이터 계승을 위한 필드 추가
        public static float CurrentExperience;
        public static int CurrentLevel;
        public static List<AugmentData> PickedAugments;
        public static List<AugmentBuff> ExternalBuffs;
        public static float PlayerCurrentHealth;
        public static int WeaponLevel;
        // 와사비 알현 성공으로 무기가 바뀐 상태인지(아델린 회전 우산, 카마리온 샷건).
        public static bool WasabiWeaponConverted;
    }
}