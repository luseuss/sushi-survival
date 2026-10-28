using UnityEngine;

namespace SushiSurvival.Audio
{
    /// <summary>AudioManager의 순수 계산부 — 볼륨 합성, 소리 고르기, 재생 간격, 페이드.</summary>
    public static class AudioLogic
    {
        /// <summary>최종 볼륨 = 전체 × 분류(배경음악/효과음) × 항목 볼륨. 음소거면 0.</summary>
        public static float EffectiveVolume(float master, float category, float entry, bool muted)
        {
            if (muted) return 0f;

            return Mathf.Clamp01(master) * Mathf.Clamp01(category) * Mathf.Clamp01(entry);
        }

        /// <summary>소리 파일이 count개일 때 roll(0 이상 1 미만의 난수)로 고른 번호. 파일이 없으면 -1.</summary>
        public static int PickIndex(int count, float roll)
        {
            if (count <= 0) return -1;

            return Mathf.Clamp((int)(roll * count), 0, count - 1);
        }

        /// <summary>마지막으로 낸 지 cooldown초가 지났을 때만 다시 낼 수 있다. 처음(아직 낸 적 없음)에는 항상 낼 수 있다.</summary>
        public static bool CanPlay(float lastPlayedTime, float now, float cooldown)
            => lastPlayedTime < 0f || now - lastPlayedTime >= cooldown;

        /// <summary>pitchMin~pitchMax 사이를 roll(0~1)로 고른 음높이. 범위가 뒤집혀 있어도 작은 쪽부터 쓴다.</summary>
        public static float PickPitch(float pitchMin, float pitchMax, float roll)
        {
            float low = Mathf.Min(pitchMin, pitchMax);
            float high = Mathf.Max(pitchMin, pitchMax);
            return Mathf.Lerp(low, high, Mathf.Clamp01(roll));
        }

        /// <summary>
        /// 소리를 시작할 위치(초). 음수는 0, 파일 길이 이상이면(건너뛸 곳이 없으니) 0으로 처음부터 재생한다 — 잘못된 값 때문에
        /// 소리가 아예 안 나는 일이 없게 한다.
        /// </summary>
        public static float ClampStartTime(float startTime, float clipLength)
            => startTime <= 0f || startTime >= clipLength ? 0f : startTime;

        /// <summary>현재 값에서 목표 값 쪽으로 fadeSeconds에 1을 오갈 속도로 한 걸음 옮긴다. fadeSeconds가 0 이하면 바로 목표 값.</summary>
        public static float FadeToward(float current, float target, float fadeSeconds, float deltaTime)
        {
            if (fadeSeconds <= 0f) return target;

            return Mathf.MoveTowards(current, target, deltaTime / fadeSeconds);
        }

        /// <summary>
        /// 저장소에 올리지 않는 외부 음원(라이선스상 원본 재배포 금지)을 두는 Resources 경로. 이 이름의 파일이 있으면
        /// SoundLibrary에 연결된 곡 대신 이 곡을 쓴다. 예: Resources/Audio/External/Bgm_Story.mp3
        /// </summary>
        public static string ExternalBgmPath(BgmId id) => "Audio/External/Bgm_" + id;

        /// <summary>
        /// 동시에 낼 수 있는 소리 칸(voice) 중 어디에 새 소리를 넣을지. 비어 있는(재생 중이 아닌) 칸이 있으면 그중 첫 번째,
        /// 모두 재생 중이면 가장 오래전에 시작한 칸을 가로챈다. startTimes[i]는 칸 i의 시작 시각, playing[i]는 재생 중 여부.
        /// </summary>
        public static int PickVoice(bool[] playing, float[] startTimes)
        {
            if (playing == null || playing.Length == 0) return -1;

            int oldest = 0;
            for (int i = 0; i < playing.Length; i++)
            {
                if (!playing[i]) return i;
                if (startTimes[i] < startTimes[oldest]) oldest = i;
            }

            return oldest;
        }
    }
}
