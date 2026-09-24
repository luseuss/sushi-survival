using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.Data
{
    /// <summary>
    /// 부스용 무입력 자동 리셋 설정. Resources 폴더의 BoothResetSettings 에셋을 인스펙터에서 고치면 된다.
    /// 에셋이 없으면 기본값으로 동작한다.
    /// </summary>
    [CreateAssetMenu(menuName = "SushiSurvival/Booth Reset Settings", fileName = "BoothResetSettings")]
    public class BoothResetSettings : ScriptableObject
    {
        [Tooltip("끄면 자동 리셋을 하지 않는다.")]
        public bool enabled = true;

        [Tooltip("유니티 에디터에서도 동작시킬지. 개발 중 테스트할 때 60초만 가만히 있어도 인트로로 튕기면 불편해서 기본은 꺼 둔다. 부스 빌드에서는 이 값과 상관없이 동작한다.")]
        public bool activeInEditor;

        [Tooltip("스토리·캐릭터 선택·호감도 대화처럼 읽고 고르는 화면에서 입력이 없을 때 기다리는 시간(초). 0 이하면 리셋하지 않는다.")]
        public float menuIdleSeconds = 60f;

        [Tooltip("전투 중 입력이 없을 때 기다리는 시간(초). 정지·팝업 중에도 센다.")]
        public float playingIdleSeconds = 60f;

        [Tooltip("결과 화면에서 입력이 없을 때 기다리는 시간(초). 볼 것을 다 본 뒤라 짧게 잡는다.")]
        public float resultIdleSeconds = 30f;

        [Tooltip("리셋 몇 초 전부터 \"곧 처음 화면으로 돌아갑니다\" 안내를 띄울지. 0 이하면 안내 없이 바로 리셋한다. 입력하면 안내가 사라진다.")]
        public float warningSeconds = 10f;

        [Tooltip("안내 글자에 쓸 폰트. 비우면 유니티 기본 폰트를 쓴다.")]
        public Font warningFont;

        public IdleTimeouts ToTimeouts() => new IdleTimeouts(menuIdleSeconds, playingIdleSeconds, resultIdleSeconds);
    }
}
