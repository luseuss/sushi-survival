using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 같은 그림의 UI 타일 여러 장을 가로로 이어 붙여 왼쪽으로 계속 흘려보낸다(횡스크롤 땅).
    /// 타일은 모두 같은 폭이어야 하고, 화면 폭보다 넓게 두 장 이상 필요하다. 실시간 시간을 써서 시간이 멈춰도 움직인다.
    /// </summary>
    public class ScrollingGround : MonoBehaviour
    {
        [Tooltip("왼쪽부터 순서대로 놓을 타일. 모두 같은 폭, 왼쪽 아래 기준(pivot 0,0)으로 둔다.")]
        [SerializeField] private RectTransform[] tiles;
        [Tooltip("초당 왼쪽으로 흐르는 거리(캔버스 단위).")]
        [SerializeField] private float speed = 220f;

        private float _offset;

        private void LateUpdate()
        {
            if (tiles == null || tiles.Length == 0 || tiles[0] == null) return;

            float width = tiles[0].rect.width;
            _offset = ScrollingGroundLogic.Advance(_offset, speed * Time.unscaledDeltaTime, width);

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null) continue;

                Vector2 position = tiles[i].anchoredPosition;
                position.x = i * width - _offset;
                tiles[i].anchoredPosition = position;
            }
        }
    }
}
