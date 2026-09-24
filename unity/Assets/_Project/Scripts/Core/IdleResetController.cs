using System;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 입력이 없었던 시간을 세고, 화면 성격별 대기 시간이 지나면 리셋을 요청한다. UnityEngine에 의존하지 않고
    /// 시간과 입력 여부를 밖에서 받아서, 시간이 흐른 상황을 테스트가 그대로 재현할 수 있다.
    /// </summary>
    public sealed class IdleResetController
    {
        private readonly Func<IdleContext> _contextProvider;
        private readonly Func<IdleTimeouts> _timeoutsProvider;
        private readonly Action _reset;
        private IdleContext _lastContext;

        public float IdleSeconds { get; private set; }

        /// <summary>마지막 Tick 기준 리셋까지 남은 시간(초). 리셋하지 않는 화면이면 음수.</summary>
        public float RemainingSeconds { get; private set; } = -1f;

        public IdleResetController(Func<IdleContext> contextProvider, Func<IdleTimeouts> timeoutsProvider, Action reset)
        {
            _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
            _timeoutsProvider = timeoutsProvider ?? throw new ArgumentNullException(nameof(timeoutsProvider));
            _reset = reset ?? throw new ArgumentNullException(nameof(reset));
        }

        /// <summary>입력이 있었다고 알린다. 화면이 바뀐 것도 활동으로 본다.</summary>
        public void NotifyActivity() => IdleSeconds = 0f;

        /// <summary>dt초가 지났다. 그 사이 입력이 있었으면 hadInput을 true로 넘긴다.</summary>
        public void Tick(float dt, bool hadInput)
        {
            IdleContext context = _contextProvider();

            // 화면 성격이 바뀌면(전투 → 결과 등) 새 화면 기준으로 처음부터 센다. 안 그러면 전투 중 쌓인
            // 무입력 시간이 결과 화면에 그대로 넘어가 결과를 보자마자 리셋된다.
            if (context != _lastContext)
            {
                _lastContext = context;
                IdleSeconds = 0f;
            }

            float timeout = IdleResetLogic.TimeoutFor(context, _timeoutsProvider());

            if (hadInput || dt < 0f || context == IdleContext.Ignore)
            {
                IdleSeconds = 0f;
                RemainingSeconds = IdleResetLogic.RemainingSeconds(0f, timeout);
                return;
            }

            IdleSeconds += dt;

            if (IdleResetLogic.IsExpired(IdleSeconds, timeout))
            {
                IdleSeconds = 0f;
                RemainingSeconds = IdleResetLogic.RemainingSeconds(0f, timeout);
                _reset();
                return;
            }

            RemainingSeconds = IdleResetLogic.RemainingSeconds(IdleSeconds, timeout);
        }
    }
}
