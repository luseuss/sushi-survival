namespace SushiSurvival.Core
{
    /// <summary>
    /// 연습 플레이(튜토리얼) 안내를 방문자마다 한 번 보여 주기 위한 기억. 파일에 저장하지 않고 실행 중에만 기억한다
    /// (부스는 저장이 없다). 방문자가 자리를 떠나 처음 화면으로 돌아오면(BoothIdleReset) 다시 잊어서 다음 방문자가
    /// 안내를 다시 보게 한다. "다시 하기"처럼 같은 방문자가 처음 화면으로 돌아온 경우에는 잊지 않는다.
    /// </summary>
    public static class TutorialState
    {
        /// <summary>이 방문자가 연습 안내에서 이미 고른 적이 있는지(연습하기든 건너뛰기든).</summary>
        public static bool SeenThisVisitor { get; private set; }

        /// <summary>메인화면에서 연습 플레이를 골라 GameScene이 연습 모드로 시작해야 하는지.</summary>
        public static bool Requested { get; private set; }

        /// <summary>
        /// 연습 플레이가 진행 중인지. 연습이 시작되는 순간(요청을 읽을 때)부터 끝나거나 처음 화면으로 돌아갈 때까지 true.
        /// 설명창 자동 열림처럼 연습 중에 꺼 두어야 하는 것들이 이 값을 본다.
        /// </summary>
        public static bool Active { get; private set; }

        /// <summary>"게임 시작"을 눌렀을 때 연습 안내를 띄워야 하는지.</summary>
        public static bool ShouldPrompt => !SeenThisVisitor;

        public static void RequestTutorial()
        {
            Requested = true;
            SeenThisVisitor = true;
        }

        /// <summary>GameScene이 연습 모드로 시작하면서 부른다. 한 번 읽으면 요청이 사라진다.</summary>
        public static bool ConsumeRequest()
        {
            bool requested = Requested;
            Requested = false;
            if (requested) Active = true;
            return requested;
        }

        /// <summary>연습이 끝났거나 건너뛰었을 때 부른다.</summary>
        public static void EndTutorial() => Active = false;

        public static void MarkSeen() => SeenThisVisitor = true;

        public static void ResetForNewVisitor()
        {
            SeenThisVisitor = false;
            Requested = false;
            Active = false;
        }
    }
}
