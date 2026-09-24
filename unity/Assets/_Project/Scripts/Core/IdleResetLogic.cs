namespace SushiSurvival.Core
{
    /// <summary>지금 화면이 어떤 성격인지. 성격에 따라 입력이 없을 때 기다리는 시간이 다르다.</summary>
    public enum IdleContext
    {
        /// <summary>이미 처음 화면(인트로)이거나 리셋 대상이 아닌 화면.</summary>
        Ignore,
        /// <summary>스토리, 캐릭터 선택, 호감도 대화처럼 읽고 고르는 화면.</summary>
        Menu,
        /// <summary>전투 중.</summary>
        Playing,
        /// <summary>결과 화면. 볼 것을 다 본 뒤라 가장 짧게 기다린다.</summary>
        Result
    }

    /// <summary>화면 성격별 무입력 대기 시간(초). 0 이하면 그 화면에서는 자동 리셋을 하지 않는다.</summary>
    public readonly struct IdleTimeouts
    {
        public readonly float Menu;
        public readonly float Playing;
        public readonly float Result;

        public IdleTimeouts(float menu, float playing, float result)
        {
            Menu = menu;
            Playing = playing;
            Result = result;
        }
    }

    /// <summary>
    /// 부스용 무입력 자동 리셋의 판정. 관람객이 자리를 떠나면 화면이 스토리·전투·결과 어디에 멈춰 있든
    /// 처음 화면으로 돌려서 다음 관람객이 바로 시작할 수 있게 한다.
    /// </summary>
    public static class IdleResetLogic
    {
        public const string IntroScene = "IntroScene";
        public const string StoryScene = "StoryScene";
        public const string GameScene = "GameScene";
        public const string BossScene = "BossScene";

        /// <param name="hasRunState">GameManager가 있어 state가 유효한지. 스토리 씬 등에는 없다.</param>
        public static IdleContext ContextFor(string sceneName, bool hasRunState, RunState state)
        {
            switch (sceneName)
            {
                case StoryScene:
                    return IdleContext.Menu;

                case GameScene:
                case BossScene:
                    if (!hasRunState) return IdleContext.Menu;

                    switch (state)
                    {
                        case RunState.Playing: return IdleContext.Playing;
                        case RunState.Result: return IdleContext.Result;
                        default: return IdleContext.Menu; // 캐릭터 선택, 호감도 대화
                    }

                default:
                    return IdleContext.Ignore;
            }
        }

        public static float TimeoutFor(IdleContext context, IdleTimeouts timeouts)
        {
            switch (context)
            {
                case IdleContext.Menu: return timeouts.Menu;
                case IdleContext.Playing: return timeouts.Playing;
                case IdleContext.Result: return timeouts.Result;
                default: return 0f;
            }
        }

        public static bool IsExpired(float idleSeconds, float timeoutSeconds)
            => timeoutSeconds > 0f && idleSeconds >= timeoutSeconds;

        /// <summary>리셋까지 남은 시간(초). 리셋하지 않는 화면이면 음수.</summary>
        public static float RemainingSeconds(float idleSeconds, float timeoutSeconds)
            => timeoutSeconds > 0f ? timeoutSeconds - idleSeconds : -1f;

        /// <summary>리셋이 임박해 "곧 돌아갑니다" 안내를 띄울 때인지. warnSeconds가 0 이하면 안내하지 않는다.</summary>
        public static bool ShouldWarn(float remainingSeconds, float warnSeconds)
            => warnSeconds > 0f && remainingSeconds >= 0f && remainingSeconds <= warnSeconds;

        /// <summary>안내에 보여줄 숫자. 0.3초 남았어도 "0"이 아니라 "1"로 보이게 올림한다.</summary>
        public static int CountdownNumber(float remainingSeconds)
            => remainingSeconds <= 0f ? 0 : (int)System.Math.Ceiling(remainingSeconds);
    }
}
