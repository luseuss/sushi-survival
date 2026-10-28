using System;
using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.Audio
{
    /// <summary>
    /// 게임의 모든 소리를 맡는 관리자. 씬에 배치하지 않고 게임이 시작될 때 스스로 하나 만들어 씬이 바뀌어도 남는다.
    ///
    /// - 배경음악: 하나씩 재생하고, 바꿀 때는 이전 곡이 서서히 줄고 새 곡이 서서히 커진다(크로스페이드).
    /// - 효과음: 동시에 여러 개가 겹쳐 난다(16칸). 모두 차 있으면 가장 오래된 소리를 끊고 쓴다.
    ///   같은 효과음은 cooldown보다 짧은 간격으로는 다시 나지 않는다.
    /// - 볼륨: 전체·배경음악·효과음 세 가지와 음소거. 값은 PlayerPrefs에 저장해 다음에 켜도 유지한다(게임 진행과는 무관).
    ///
    /// 소리 이름과 파일의 연결은 Resources/Audio/SoundLibrary 에셋이 맡고, 파일이 없는 이름은 조용히 넘어간다.
    /// 호출하는 쪽은 AudioManager.PlaySfx(SfxId.EnemyHit) 처럼 정적 메서드만 쓰면 된다 — 관리자가 아직 없어도 안전하다.
    /// 시간은 실시간(unscaled)을 쓴다 — 팝업·히트스톱으로 timeScale이 0이어도 소리와 페이드는 이어진다.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const string LibraryResourcePath = "Audio/SoundLibrary";
        private const int SfxVoiceCount = 16;

        private const string MasterKey = "audio.master";
        private const string BgmKey = "audio.bgm";
        private const string SfxKey = "audio.sfx";
        private const string MutedKey = "audio.muted";

        public static AudioManager Instance { get; private set; }

        /// <summary>지금 나오는(또는 페이드로 들어오는) 배경음악 이름. 이름 없이 파일로 튼 곡이나 멈춘 상태는 None이다. 잠깐 다른 곡으로 바꿨다가 돌아올 때 쓴다.</summary>
        public static BgmId CurrentBgm => Instance != null ? Instance._currentBgmId : BgmId.None;

        /// <summary>볼륨이나 음소거가 바뀔 때 알린다. 설정 화면의 슬라이더 표시를 맞출 때 쓴다.</summary>
        public static event Action VolumeChanged;

        // ---------- 설정 ----------

        private static float _masterVolume = 1f;
        private static float _bgmVolume = 0.7f;
        private static float _sfxVolume = 1f;
        private static bool _muted;
        private static bool _settingsLoaded;

        public static float MasterVolume
        {
            get { EnsureSettingsLoaded(); return _masterVolume; }
            set { SetVolume(ref _masterVolume, value, MasterKey); }
        }

        public static float BgmVolume
        {
            get { EnsureSettingsLoaded(); return _bgmVolume; }
            set { SetVolume(ref _bgmVolume, value, BgmKey); }
        }

        public static float SfxVolume
        {
            get { EnsureSettingsLoaded(); return _sfxVolume; }
            set { SetVolume(ref _sfxVolume, value, SfxKey); }
        }

        public static bool Muted
        {
            get { EnsureSettingsLoaded(); return _muted; }
            set
            {
                EnsureSettingsLoaded();
                if (_muted == value) return;

                _muted = value;
                PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
                VolumeChanged?.Invoke();
            }
        }

        private static void EnsureSettingsLoaded()
        {
            if (_settingsLoaded) return;

            _settingsLoaded = true;
            _masterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
            _bgmVolume = PlayerPrefs.GetFloat(BgmKey, 0.7f);
            _sfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
            _muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
        }

        private static void SetVolume(ref float field, float value, string key)
        {
            EnsureSettingsLoaded();

            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(field, value)) return;

            field = value;
            PlayerPrefs.SetFloat(key, value);
            VolumeChanged?.Invoke();
        }

        // ---------- 정적 호출 창구 ----------

        /// <summary>이름으로 효과음을 낸다. 라이브러리에 파일이 없으면 아무 일도 없다.</summary>
        public static void PlaySfx(SfxId id)
        {
            if (Instance != null) Instance.PlaySfxById(id);
        }

        /// <summary>소리 파일을 직접 효과음으로 낸다. 라이브러리를 거치지 않는다.</summary>
        public static void PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (Instance != null) Instance.PlaySfxClip(clip, volume, pitch);
        }

        /// <summary>이름으로 배경음악을 바꾼다. 이미 같은 곡이 나오고 있으면 그대로 둔다.</summary>
        public static void PlayBgm(BgmId id, float fadeSeconds = 0.8f)
        {
            if (Instance != null) Instance.PlayBgmById(id, fadeSeconds);
        }

        /// <summary>소리 파일을 직접 배경음악으로 튼다.</summary>
        public static void PlayBgm(AudioClip clip, float volume = 1f, float fadeSeconds = 0.8f, bool loop = true)
        {
            if (Instance != null) Instance.PlayBgmClip(clip, volume, loop, fadeSeconds);
        }

        /// <summary>배경음악을 서서히 줄여 멈춘다.</summary>
        public static void StopBgm(float fadeSeconds = 0.8f)
        {
            if (Instance != null) Instance.FadeOutAllBgm(fadeSeconds);
        }

        // ---------- 인스턴스 ----------

        private sealed class BgmChannel
        {
            public AudioSource Source;
            public float Level;       // 페이드 진행(0~1)
            public float Target;      // 페이드 목표(0 또는 1)
            public float FadeSeconds; // 이 채널이 목표로 가는 데 걸리는 시간
            public float EntryVolume; // 이 곡의 라이브러리 볼륨
        }

        private SoundLibrary _library;
        private AudioSource[] _sfxVoices;
        private float[] _voiceStartTimes;
        private bool[] _voicePlaying;
        private readonly Dictionary<SfxId, float> _lastPlayed = new Dictionary<SfxId, float>();

        private BgmChannel[] _channels;
        private int _current;
        private AudioClip _currentClip;
        private BgmId _currentBgmId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            var host = new GameObject("AudioManager");
            DontDestroyOnLoad(host);
            host.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _library = Resources.Load<SoundLibrary>(LibraryResourcePath);
            if (_library == null)
                Debug.Log($"[AudioManager] Resources/{LibraryResourcePath} 에셋이 없어 효과음·배경음악은 재생하지 않습니다.");

            _sfxVoices = new AudioSource[SfxVoiceCount];
            _voiceStartTimes = new float[SfxVoiceCount];
            _voicePlaying = new bool[SfxVoiceCount];
            for (int i = 0; i < SfxVoiceCount; i++)
                _sfxVoices[i] = CreateSource($"Sfx_{i}", loop: false);

            _channels = new[]
            {
                new BgmChannel { Source = CreateSource("Bgm_A", loop: true) },
                new BgmChannel { Source = CreateSource("Bgm_B", loop: true) },
            };
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);

            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // 2D 소리 — 화면 위치와 무관하게 똑같이 들린다.
            return source;
        }

        // 재생을 시작한 소리를 startTime(초) 지점으로 옮긴다. 0이면 그대로 둔다.
        private static void SeekTo(AudioSource source, float startTime)
        {
            float start = AudioLogic.ClampStartTime(startTime, source.clip.length);
            if (start > 0f) source.time = start;
        }

        // ---------- 효과음 ----------

        private void PlaySfxById(SfxId id)
        {
            if (_library == null || !_library.TryGetSfx(id, out SfxEntry entry)) return;
            if (entry.clips == null || entry.clips.Length == 0) return;

            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(id, out float last) && !AudioLogic.CanPlay(last, now, entry.cooldown)) return;

            AudioClip clip = entry.clips[AudioLogic.PickIndex(entry.clips.Length, UnityEngine.Random.value)];
            if (clip == null) return;

            _lastPlayed[id] = now;
            PlayOnVoice(clip, entry.volume, AudioLogic.PickPitch(entry.pitchMin, entry.pitchMax, UnityEngine.Random.value), entry.startTime);
        }

        private void PlaySfxClip(AudioClip clip, float volume, float pitch)
        {
            if (clip == null) return;

            PlayOnVoice(clip, volume, pitch);
        }

        private void PlayOnVoice(AudioClip clip, float entryVolume, float pitch, float startTime = 0f)
        {
            for (int i = 0; i < SfxVoiceCount; i++)
                _voicePlaying[i] = _sfxVoices[i].isPlaying;

            int index = AudioLogic.PickVoice(_voicePlaying, _voiceStartTimes);
            if (index < 0) return;

            AudioSource voice = _sfxVoices[index];
            voice.Stop();
            voice.clip = clip;
            voice.pitch = pitch;
            voice.volume = AudioLogic.EffectiveVolume(MasterVolume, SfxVolume, entryVolume, Muted);
            voice.Play();
            SeekTo(voice, startTime);
            _voiceStartTimes[index] = Time.unscaledTime;
        }

        // ---------- 배경음악 ----------

        private void PlayBgmById(BgmId id, float fadeSeconds)
        {
            if (id == BgmId.None)
            {
                FadeOutAllBgm(fadeSeconds);
                return;
            }

            if (_library == null || !_library.TryGetBgm(id, out BgmEntry entry) || entry.clip == null) return;

            PlayBgmClip(entry.clip, entry.volume, entry.loop, fadeSeconds, entry.startTime);
            _currentBgmId = id;
        }

        private void PlayBgmClip(AudioClip clip, float volume, bool loop, float fadeSeconds, float startTime = 0f)
        {
            if (clip == null) return;

            BgmChannel current = _channels[_current];

            // 이미 이 곡이 나오고 있으면 다시 시작하지 않고 볼륨만 맞춘다.
            if (_currentClip == clip && current.Source.isPlaying && current.Target > 0f)
            {
                current.EntryVolume = volume;
                return;
            }

            // 이전 곡은 서서히 줄이고, 비어 있는 채널에서 새 곡을 서서히 키운다.
            current.Target = 0f;
            current.FadeSeconds = fadeSeconds;

            _current = 1 - _current;
            BgmChannel next = _channels[_current];
            next.Source.Stop();
            next.Source.clip = clip;
            next.Source.loop = loop;
            next.EntryVolume = volume;
            next.Level = fadeSeconds > 0f ? 0f : 1f;
            next.Target = 1f;
            next.FadeSeconds = fadeSeconds;
            next.Source.volume = 0f;
            next.Source.Play();
            SeekTo(next.Source, startTime);
            _currentClip = clip;
            _currentBgmId = BgmId.None;
        }

        private void FadeOutAllBgm(float fadeSeconds)
        {
            foreach (BgmChannel channel in _channels)
            {
                channel.Target = 0f;
                channel.FadeSeconds = fadeSeconds;
            }

            _currentClip = null;
            _currentBgmId = BgmId.None;
        }

        private void Update()
        {
            if (_channels == null) return;

            float deltaTime = Time.unscaledDeltaTime;
            foreach (BgmChannel channel in _channels)
            {
                if (!channel.Source.isPlaying && channel.Level <= 0f) continue;

                channel.Level = AudioLogic.FadeToward(channel.Level, channel.Target, channel.FadeSeconds, deltaTime);
                channel.Source.volume = channel.Level
                    * AudioLogic.EffectiveVolume(MasterVolume, BgmVolume, channel.EntryVolume, Muted);

                // 완전히 사라진 채널은 멈춘다. 볼륨 설정이 0이어도 곡은 계속 흘러가야 하므로 페이드 값만 본다.
                if (channel.Target <= 0f && channel.Level <= 0f)
                    channel.Source.Stop();
            }
        }
    }
}
