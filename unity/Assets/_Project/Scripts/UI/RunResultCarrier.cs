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
        public static float PlayerCurrentHealth;
        public static int WeaponLevel;
    }
}