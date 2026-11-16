using UnityEngine;

namespace SushiSurvival.Pickups
{
    /// <summary>
    /// 스프라이트 뒤에 같은 그림을 어둡게 깔아 그림자를 만든다. 자식 오브젝트를 런타임에 만들고
    /// 매 프레임 스프라이트·정렬을 따라가므로, 젬처럼 풀에서 재사용되거나 스케일이 바뀌어도 그대로 따라온다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteDropShadow : MonoBehaviour
    {
        [Tooltip("그림자가 본체에서 벗어나는 거리(본체 기준 좌표). 아래쪽(-y)·오른쪽(+x)이 자연스럽다.")]
        [SerializeField] private Vector2 offset = new Vector2(0.06f, -0.06f);
        [Tooltip("그림자 색. 알파로 진하기를 조절한다.")]
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.55f);
        [Tooltip("본체보다 이만큼 뒤에 그린다(Order in Layer). 땅에 가려지면 값을 0으로 올리고 본체를 1로 올린다.")]
        [SerializeField] private int orderOffset = -1;

        private SpriteRenderer _source;
        private SpriteRenderer _shadow;

        private void Awake()
        {
            _source = GetComponent<SpriteRenderer>();

            var shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(transform, false);
            shadowObject.transform.localPosition = offset;

            _shadow = shadowObject.AddComponent<SpriteRenderer>();
            _shadow.color = shadowColor;
        }

        private void LateUpdate()
        {
            _shadow.sprite = _source.sprite;
            _shadow.flipX = _source.flipX;
            _shadow.flipY = _source.flipY;
            _shadow.sortingLayerID = _source.sortingLayerID;
            _shadow.sortingOrder = _source.sortingOrder + orderOffset;
            _shadow.enabled = _source.enabled;
        }
    }
}
