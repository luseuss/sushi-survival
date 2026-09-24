using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 캐릭터 선택 화면이 열릴 때 카드가 아래에서 차례로 올라오며 나타나고, 그동안(과 잠깐 더) 클릭을 막는다.
    /// 카드 세 장이 화면 전체를 덮고 있어서, 스토리를 연타하다 이 장면으로 넘어오면 그 클릭이 그대로
    /// 카드에 꽂혀 원치 않는 캐릭터가 골라졌다. 잠금은 interactable이 아니라 blocksRaycasts로 건다 —
    /// interactable은 클릭이 "눌렀을 때"가 아니라 "뗐을 때" 검사돼서, 잠긴 동안 눌러 시작하고 풀린 뒤에
    /// 떼면 클릭으로 잡히기 때문이다. 패널에 런타임으로 붙이므로 씬 배선이 필요 없다.
    /// </summary>
    public class CharacterSelectEntrance : MonoBehaviour
    {
        [Tooltip("카드 한 장이 올라오는 데 걸리는 실시간(초).")]
        [SerializeField] private float cardDuration = 0.4f;
        [Tooltip("카드 사이 등장 시차(초). 왼쪽 카드부터 차례로 나타난다.")]
        [SerializeField] private float stagger = 0.15f;
        [Tooltip("등장이 끝난 뒤 클릭을 더 막는 시간(초). 스토리 연타의 마지막 클릭들이 지나가도록 여유를 둔다.")]
        [SerializeField] private float holdAfter = 0.3f;
        [Tooltip("카드가 시작하는 위치가 원래 자리보다 얼마나 아래인지(픽셀).")]
        [SerializeField] private float slideDistance = 90f;

        private sealed class Card
        {
            public RectTransform Rect;
            public CanvasGroup Group;
            public Vector2 BasePosition;
        }

        private readonly List<Card> _cards = new List<Card>();
        private CanvasGroup _panelGroup;
        private float _elapsed;
        private float _unlockTime;
        private bool _playing;

        public bool IsLocked => _playing;

        /// <summary>패널에 연출 컴포넌트를 붙이고 돌려준다. 이미 있으면 그것을 쓴다.</summary>
        public static CharacterSelectEntrance Attach(GameObject panel)
        {
            return panel.TryGetComponent<CharacterSelectEntrance>(out var existing)
                ? existing
                : panel.AddComponent<CharacterSelectEntrance>();
        }

        public void Play()
        {
            if (_playing) Finish();

            _cards.Clear();
            foreach (var button in GetComponentsInChildren<CharacterSelectButton>(true))
            {
                var rect = (RectTransform)button.transform;
                if (!button.TryGetComponent<CanvasGroup>(out var group))
                    group = button.gameObject.AddComponent<CanvasGroup>();

                _cards.Add(new Card { Rect = rect, Group = group, BasePosition = rect.anchoredPosition });
            }

            // 화면 왼쪽 카드부터 나타나게 가로 위치순으로 정렬한다.
            _cards.Sort((a, b) => a.Rect.position.x.CompareTo(b.Rect.position.x));

            if (!TryGetComponent(out _panelGroup))
                _panelGroup = gameObject.AddComponent<CanvasGroup>();

            _panelGroup.interactable = false;
            _panelGroup.blocksRaycasts = false;

            _elapsed = 0f;
            _unlockTime = EntranceLogic.UnlockTime(_cards.Count, stagger, cardDuration, holdAfter);
            _playing = true;

            Apply();
        }

        // 장면 로딩 직후 첫 프레임은 로딩 시간이 통째로 들어와 dt가 클 수 있다. 그대로 쓰면 연출과 잠금이
        // 한 프레임에 끝나버려서 막으려던 클릭이 그대로 통과하므로 프레임당 진행량에 상한을 둔다.
        private void Update() => Tick(Mathf.Min(Time.unscaledDeltaTime, MaxFrameStep));

        private const float MaxFrameStep = 0.05f;

        /// <summary>시간을 dt만큼 진행한다. Update가 부르며, 테스트가 시간을 직접 밀 수 있게 공개해 둔다.</summary>
        public void Tick(float dt)
        {
            if (!_playing) return;

            _elapsed += dt;
            Apply();

            if (EntranceLogic.IsUnlocked(_elapsed, _unlockTime))
                Finish();
        }

        private void Apply()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                float t = EntranceLogic.CardProgress(_elapsed, i, stagger, cardDuration);
                float eased = EntranceLogic.EaseOutCubic(t);

                _cards[i].Group.alpha = eased;
                _cards[i].Rect.anchoredPosition = _cards[i].BasePosition + Vector2.down * (slideDistance * (1f - eased));
            }
        }

        // 도중에 패널이 꺼져도 카드가 반쯤 내려간 채로, 클릭이 막힌 채로 남지 않게 원상복구한다.
        private void OnDisable()
        {
            if (_playing) Finish();
        }

        private void Finish()
        {
            foreach (var card in _cards)
            {
                card.Group.alpha = 1f;
                card.Rect.anchoredPosition = card.BasePosition;
            }

            if (_panelGroup != null)
            {
                _panelGroup.interactable = true;
                _panelGroup.blocksRaycasts = true;
            }

            _playing = false;
        }
    }
}
