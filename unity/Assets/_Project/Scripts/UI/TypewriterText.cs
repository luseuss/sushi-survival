using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// Text에 대사를 한 글자씩 찍어 보여준다. 패널이 런타임에 Text에 붙이므로 씬 배선이 필요 없다.
    /// 대화 중엔 timeScale이 0일 수 있어 unscaled 시간으로 진행한다.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class TypewriterText : MonoBehaviour
    {
        private Text _text;
        private string _full = string.Empty;
        private int _totalVisible;
        private float _charsPerSecond;
        private float _elapsed;

        public bool IsPlaying { get; private set; }

        private void Awake() => _text = GetComponent<Text>();

        public void Play(string text, float charsPerSecond)
        {
            if (_text == null) _text = GetComponent<Text>();

            _full = text ?? string.Empty;
            _totalVisible = TypewriterLogic.CountVisible(_full);
            _charsPerSecond = charsPerSecond;
            _elapsed = 0f;

            if (charsPerSecond <= 0f || _totalVisible == 0)
            {
                Finish();
                return;
            }

            IsPlaying = true;
            _text.text = string.Empty;
        }

        /// <summary>진행 중이던 타이핑을 즉시 끝내고 전체 문장을 보여준다.</summary>
        public void Complete()
        {
            if (IsPlaying) Finish();
        }

        private void Update()
        {
            if (!IsPlaying) return;

            _elapsed += Time.unscaledDeltaTime;
            int revealed = TypewriterLogic.RevealedCount(_elapsed, _charsPerSecond, _totalVisible);

            if (revealed >= _totalVisible)
            {
                Finish();
                return;
            }

            _text.text = TypewriterLogic.Substring(_full, revealed);
        }

        // 타이핑 도중 패널이 꺼져도 다음에 켜질 때 반쯤 찍힌 문장이 남지 않게 한다.
        private void OnDisable()
        {
            if (IsPlaying) Finish();
        }

        private void Finish()
        {
            IsPlaying = false;
            if (_text != null) _text.text = _full;
        }
    }
}
