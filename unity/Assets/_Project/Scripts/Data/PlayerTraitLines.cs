using System;
using UnityEngine;

namespace SushiSurvival.Data
{
    [Serializable]
    public class PlayerTraitEntry
    {
        public PlayerTrait trait;
        [Tooltip("화면에 보이는 성향 이름(용감함 등).")]
        public string displayName;
        [TextArea] public string victoryLine;
        [TextArea] public string defeatLine;
    }

    /// <summary>
    /// 성향별 표시명과 결과 화면 한마디. Resources 폴더에 두어 씬 배선 없이 어디서든 불러 쓴다
    /// (BoothResetSettings와 같은 방식).
    /// </summary>
    [CreateAssetMenu(menuName = "SushiSurvival/Player Trait Lines", fileName = "PlayerTraitLines")]
    public class PlayerTraitLines : ScriptableObject
    {
        public const string ResourcePath = "PlayerTraitLines";

        public PlayerTraitEntry[] entries;

        /// <summary>해당 성향의 항목. None이거나 없으면 null.</summary>
        public PlayerTraitEntry Find(PlayerTrait trait)
        {
            if (trait == PlayerTrait.None || entries == null) return null;

            foreach (PlayerTraitEntry entry in entries)
            {
                if (entry != null && entry.trait == trait) return entry;
            }

            return null;
        }

        /// <summary>Resources에서 불러온다. 에셋이 없으면 null.</summary>
        public static PlayerTraitLines Load() => Resources.Load<PlayerTraitLines>(ResourcePath);
    }
}
