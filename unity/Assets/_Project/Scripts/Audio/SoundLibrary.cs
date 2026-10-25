using System;
using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.Audio
{
    /// <summary>효과음 하나의 설정. 소리 파일을 여러 개 넣으면 재생할 때마다 무작위로 하나를 고른다(같은 소리가 반복돼 지루해지는 걸 막는다).</summary>
    [Serializable]
    public class SfxEntry
    {
        public SfxId id;
        [Tooltip("이 효과음에 쓸 소리 파일. 여러 개면 재생할 때마다 무작위로 고른다.")]
        public AudioClip[] clips;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Tooltip("재생할 때마다 음높이를 이 범위에서 무작위로 바꾼다. 둘 다 1이면 그대로.")]
        public float pitchMin = 1f;
        public float pitchMax = 1f;
        [Tooltip("같은 효과음을 다시 낼 수 있기까지의 최소 간격(초). 광역 공격으로 몹 수십 마리가 한꺼번에 맞아도 소리가 겹쳐 터지지 않게 한다.")]
        public float cooldown = 0.05f;
    }

    [Serializable]
    public class BgmEntry
    {
        public BgmId id;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        public bool loop = true;
    }

    /// <summary>
    /// 게임의 모든 소리를 이름(SfxId·BgmId)에 연결해 두는 목록. Resources/Audio/SoundLibrary 에셋 하나만 쓴다.
    /// 소리 파일을 프로젝트에 넣고 이 에셋에서 해당 이름의 칸에 끌어다 놓으면 코드 수정 없이 소리가 난다.
    /// 파일이 비어 있는 이름은 조용히 넘어간다.
    /// </summary>
    [CreateAssetMenu(menuName = "SushiSurvival/Sound Library", fileName = "SoundLibrary")]
    public class SoundLibrary : ScriptableObject
    {
        [SerializeField] private SfxEntry[] sfx = Array.Empty<SfxEntry>();
        [SerializeField] private BgmEntry[] bgm = Array.Empty<BgmEntry>();

        private Dictionary<SfxId, SfxEntry> _sfxById;
        private Dictionary<BgmId, BgmEntry> _bgmById;

        public bool TryGetSfx(SfxId id, out SfxEntry entry)
        {
            _sfxById ??= BuildLookup(sfx, e => e.id);
            return _sfxById.TryGetValue(id, out entry);
        }

        public bool TryGetBgm(BgmId id, out BgmEntry entry)
        {
            _bgmById ??= BuildLookup(bgm, e => e.id);
            return _bgmById.TryGetValue(id, out entry);
        }

        // 같은 이름이 두 번 적혀 있으면 앞의 것을 쓴다.
        private static Dictionary<TKey, TEntry> BuildLookup<TKey, TEntry>(TEntry[] entries, Func<TEntry, TKey> keyOf)
        {
            var lookup = new Dictionary<TKey, TEntry>();
            if (entries == null) return lookup;

            foreach (TEntry entry in entries)
            {
                if (entry == null) continue;
                TKey key = keyOf(entry);
                if (!lookup.ContainsKey(key)) lookup.Add(key, entry);
            }

            return lookup;
        }

        private void OnValidate()
        {
            // 인스펙터에서 목록을 고치면 다음 조회 때 다시 만든다.
            _sfxById = null;
            _bgmById = null;
        }
    }
}
