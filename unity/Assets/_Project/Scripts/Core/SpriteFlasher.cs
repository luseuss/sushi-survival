using System.Collections;
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// SpriteRenderer의 색 연출을 한 곳에서 맡는다. 피격 플래시와 보스의 페이즈 플래시가 각자 color를
    /// 직접 건드리면 같은 프레임에 부딪혀 색이 꼬인다.
    ///
    /// 두 가지 연출이 있다.
    /// - Flash: renderer.color를 잠깐 바꾼다. 곱하기 색이라 흰색으로는 변화가 없고, 어두운 스프라이트에는
    ///   붉은 기운이 잘 안 보인다. 보스 예고 같은 "색 입히기"용이다.
    /// - HitFlash: 스프라이트를 통째로 하얗게 덮었다가 붉은 기운을 남기며 원래대로 돌아온다. 피격용.
    ///   스프라이트 색을 섞는 셰이더(SpriteFlash)가 필요해서, 처음 쓸 때 그 셰이더 재질로 바꿔 끼운다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlasher : MonoBehaviour
    {
        private const string ShaderResourcePath = "Shaders/SpriteFlash";
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

        // 모든 스프라이트가 같은 재질을 공유한다. 플래시 값은 렌더러별 MaterialPropertyBlock으로 준다.
        private static Material _flashMaterial;

        private SpriteRenderer _renderer;
        private Color _baseColor;
        private Coroutine _routine;
        private MaterialPropertyBlock _block;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _baseColor = _renderer.color;
        }

        // 플래시 도중에 꺼지면(죽어서 풀로 돌아가면) 코루틴이 멈춰 색이 번쩍인 채로 남는다.
        private void OnDisable()
        {
            _routine = null;
            if (_renderer == null) return;

            _renderer.color = _baseColor;
            _renderer.SetPropertyBlock(null);
        }

        /// <summary>진행 중이던 플래시를 취소하고 새로 시작한다 — 연속 타격
        /// 중에는 계속 번쩍인 상태로 보이는 게 맞다.</summary>
        public void Flash(Color color, float duration)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FlashRoutine(color, duration));
        }

        /// <summary>
        /// 스프라이트 전체를 하얗게 덮고 holdDuration 동안 유지한 뒤, fadeDuration 동안 fadeColor의 기운을 남기며
        /// 원래대로 돌아온다. 연속 타격 중에는 처음부터 다시 번쩍인다.
        /// </summary>
        public void HitFlash(float holdDuration, float fadeDuration, Color fadeColor)
        {
            if (!EnsureFlashMaterial()) return;

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(HitFlashRoutine(holdDuration, fadeDuration, fadeColor));
        }

        private bool EnsureFlashMaterial()
        {
            if (_renderer.sharedMaterial != null && _renderer.sharedMaterial.HasProperty(FlashAmountId))
                return true;

            if (_flashMaterial == null)
            {
                Shader shader = Resources.Load<Shader>(ShaderResourcePath);
                if (shader == null) return false;

                _flashMaterial = new Material(shader) { name = "SpriteFlash (runtime)" };
            }

            _renderer.sharedMaterial = _flashMaterial;
            return true;
        }

        private void SetFlash(Color color, float amount)
        {
            _block ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(FlashColorId, color);
            _block.SetFloat(FlashAmountId, amount);
            _renderer.SetPropertyBlock(_block);
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            _renderer.color = color;

            // 실시간으로 진행한다 — 맞는 순간이 곧 히트스톱이 걸리는 순간이라,
            // scaled 시간을 쓰면 timeScale이 0인 동안 거의 진행되지 않아 정지가
            // 풀릴 때까지 계속 하얗게 남는다.
            yield return new WaitForSecondsRealtime(duration);

            _renderer.color = _baseColor;
            _routine = null;
        }

        private IEnumerator HitFlashRoutine(float holdDuration, float fadeDuration, Color fadeColor)
        {
            SetFlash(Color.white, 1f);
            yield return new WaitForSecondsRealtime(holdDuration);

            for (float elapsed = 0f; elapsed < fadeDuration; elapsed += Time.unscaledDeltaTime)
            {
                float t = elapsed / fadeDuration;
                SetFlash(Color.Lerp(Color.white, fadeColor, t), 1f - t);
                yield return null;
            }

            // 플래시가 없을 때는 속성 블록을 비워 둔다 — 렌더러마다 블록이 있으면 배칭이 깨진다.
            _renderer.SetPropertyBlock(null);
            _routine = null;
        }
    }
}
