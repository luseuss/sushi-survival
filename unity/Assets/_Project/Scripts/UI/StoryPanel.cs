using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 선형 대화 화면의 뷰. 배경 두 장으로 크로스페이드하고, 이름표·초상화·대사를
    /// 표시한다. 무엇을 언제 보여줄지는 StorySceneController가 정한다.
    /// </summary>
    public class StoryPanel : MonoBehaviour
    {
        [Header("배경 (크로스페이드용 2장)")]
        [Tooltip("현재 배경. 하이어라키에서 Back보다 아래(나중에 그려지는 쪽)에 둔다.")]
        [SerializeField] private Image backgroundFront;
        [Tooltip("전환 대상 배경. Front 바로 뒤에 깔린다.")]
        [SerializeField] private Image backgroundBack;

        [Header("대사창")]
        [SerializeField] private GameObject nameplateRoot;
        [SerializeField] private Text nameText;
        [SerializeField] private Image miniPortrait;
        [Tooltip("화면에 크게 서 있는 입상. 비워두면 입상을 표시하지 않는다.")]
        [SerializeField] private Image standingImage;
        [SerializeField] private Text bodyText;

        private Coroutine _fade;
        private Sprite _pending;

        public void ShowLine(StoryLine line)
        {
            bool hasName = !string.IsNullOrEmpty(line.speakerName);

            if (nameplateRoot != null)
                nameplateRoot.SetActive(hasName);

            if (nameText != null)
                nameText.text = hasName ? line.speakerName : string.Empty;

            if (miniPortrait != null)
            {
                miniPortrait.sprite = line.portrait;
                miniPortrait.gameObject.SetActive(line.portrait != null);
            }

            if (standingImage != null)
            {
                standingImage.sprite = line.standing;
                standingImage.gameObject.SetActive(line.standing != null);
            }

            if (bodyText != null)
                bodyText.text = line.text;
        }

        /// <summary>페이드 없이 즉시 배경을 바꾼다. null이면 배경을 끈다(카메라 배경색이 보인다).</summary>
        public void SetBackground(Sprite sprite)
        {
            StopFade();
            ApplyBackground(sprite);
        }

        /// <summary>
        /// 배경을 페이드로 바꾼다. 이미 페이드 중이면 그것부터 즉시 끝낸다 — 빠르게 연타해도
        /// 중간 화면이 튀지 않게 하기 위해서다. 실시간(unscaled)으로 진행한다.
        /// </summary>
        public void FadeToBackground(Sprite sprite, float seconds)
        {
            if (_fade != null)
            {
                StopCoroutine(_fade);
                _fade = null;
                ApplyBackground(_pending);
            }

            if (seconds <= 0f || !isActiveAndEnabled)
            {
                ApplyBackground(sprite);
                return;
            }

            _pending = sprite;
            _fade = StartCoroutine(FadeRoutine(sprite, seconds));
        }

        private IEnumerator FadeRoutine(Sprite next, float seconds)
        {
            if (backgroundBack != null)
            {
                backgroundBack.sprite = next;
                backgroundBack.enabled = next != null;
                SetAlpha(backgroundBack, 1f);
            }

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (backgroundFront != null)
                    SetAlpha(backgroundFront, 1f - Mathf.Clamp01(elapsed / seconds));
                yield return null;
            }

            _fade = null;
            ApplyBackground(next);
        }

        private void StopFade()
        {
            if (_fade == null) return;

            StopCoroutine(_fade);
            _fade = null;
        }

        private void ApplyBackground(Sprite sprite)
        {
            if (backgroundFront != null)
            {
                backgroundFront.sprite = sprite;
                backgroundFront.enabled = sprite != null;
                SetAlpha(backgroundFront, 1f);
            }

            if (backgroundBack != null)
                backgroundBack.enabled = false;
        }

        private static void SetAlpha(Image image, float alpha)
        {
            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }
    }
}
