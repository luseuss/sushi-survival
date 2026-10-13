using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.Enemies
{
    /// <summary>
    /// PolygonCollider2D의 모양을 지금 그려진 스프라이트의 윤곽에 맞춘다. 스프라이트가 바뀌면(애니메이션 프레임·
    /// 변신) 판정도 그 프레임의 모양으로 따라 바뀐다. 모양은 스프라이트 임포트 때 만들어진 물리 모양을 쓴다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(PolygonCollider2D))]
    public class SpriteHitbox : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private PolygonCollider2D _collider;
        private Sprite _applied;
        private readonly List<Vector2> _points = new List<Vector2>();

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<PolygonCollider2D>();
            Apply(_renderer.sprite);
        }

        // 애니메이터가 이번 프레임에 스프라이트를 바꿨다면 그 뒤에 따라간다.
        private void LateUpdate()
        {
            if (_renderer.sprite != _applied)
                Apply(_renderer.sprite);
        }

        private void Apply(Sprite sprite)
        {
            _applied = sprite;
            if (sprite == null) return;

            int shapeCount = sprite.GetPhysicsShapeCount();
            if (shapeCount == 0) return;

            _collider.pathCount = shapeCount;
            for (int i = 0; i < shapeCount; i++)
            {
                sprite.GetPhysicsShape(i, _points);
                _collider.SetPath(i, _points);
            }
        }
    }
}
