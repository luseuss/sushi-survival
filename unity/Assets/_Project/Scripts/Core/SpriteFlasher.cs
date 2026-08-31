using System.Collections;
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// SpriteRenderer.color를 바꾸는 유일한 곳. 피격 플래시와 보스의 페이즈
    /// 플래시가 각자 color를 직접 건드리면 같은 프레임에 부딪혀 색이 꼬인다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlasher : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Color _baseColor;
        private Coroutine _routine;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _baseColor = _renderer.color;
        }

        /// <summary>진행 중이던 플래시를 취소하고 새로 시작한다 — 연속 타격
        /// 중에는 계속 번쩍인 상태로 보이는 게 맞다.</summary>
        public void Flash(Color color, float duration)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FlashRoutine(color, duration));
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
    }
}
