using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 세계관 대화에서 고른 성향을 씬 전환을 넘어 들고 있는다(RunResultCarrier와 같은 방식).
    /// StorySceneController가 시작할 때 Reset하고, 호감도 대화·보스 대사·결과 화면이 Dominant를 읽는다.
    /// 건너뛰기나 에디터에서 GameScene으로 직행하면 비어 있어 Dominant가 None이고, 그러면 모두 기존 동작이다.
    /// </summary>
    public static class PlayerTraitState
    {
        private static readonly List<PlayerTrait> _picks = new List<PlayerTrait>();

        public static IReadOnlyList<PlayerTrait> Picks => _picks;

        public static PlayerTrait Dominant => PlayerTraitLogic.Dominant(_picks);

        public static void Record(PlayerTrait trait)
        {
            if (trait == PlayerTrait.None) return;

            _picks.Add(trait);
        }

        public static void Reset() => _picks.Clear();
    }
}
