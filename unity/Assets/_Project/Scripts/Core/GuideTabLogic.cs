namespace SushiSurvival.Core
{
    /// <summary>캐릭터 선택 화면의 설명창 탭.</summary>
    public enum GuideTab
    {
        /// <summary>조작과 한 줄 요약. 씬에 직접 쓴 내용을 보여준다.</summary>
        Gameplay,
        /// <summary>증강 10종 목록.</summary>
        Augments,
        /// <summary>와사비 도전 설명과 펫 목록.</summary>
        Fairy
    }

    /// <summary>탭마다 무엇을 켜고 끌지. 탭을 바꿀 때 화면에 보이는 영역이 정확히 하나가 되게 한다.</summary>
    public static class GuideTabLogic
    {
        /// <summary>씬에 직접 쓴 내용(Gameplay Root)을 보여주는 탭인지.</summary>
        public static bool ShowsGameplayRoot(GuideTab tab) => tab == GuideTab.Gameplay;

        /// <summary>스크롤 목록 영역을 보여주는 탭인지.</summary>
        public static bool ShowsList(GuideTab tab) => tab != GuideTab.Gameplay;

        public static bool ShowsAugmentRows(GuideTab tab) => tab == GuideTab.Augments;

        public static bool ShowsFairyRows(GuideTab tab) => tab == GuideTab.Fairy;
    }
}
