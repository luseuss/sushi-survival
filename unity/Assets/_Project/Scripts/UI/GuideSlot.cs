using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SushiSurvival.UI
{
    /// <summary>설명창 격자의 칸 하나. 마우스를 올리거나 클릭하면 이 칸을 선택해 달라고 알린다.</summary>
    public class GuideSlot : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public int Index { get; set; }
        public Action<int> OnSelect { get; set; }

        public void OnPointerEnter(PointerEventData eventData) => OnSelect?.Invoke(Index);

        public void OnPointerClick(PointerEventData eventData) => OnSelect?.Invoke(Index);
    }
}
