
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HorizontalWheelScroll :
    MonoBehaviour, IScrollHandler
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float scrollAmount = 120f;

    public void OnScroll(PointerEventData eventData)
    {
        if (scrollRect == null)
            return;

        float availableWidth =
            scrollRect.content.rect.width -
            scrollRect.viewport.rect.width;

        if (availableWidth <= 0f)
            return;

        float movement = eventData.scrollDelta.y * scrollAmount;

        scrollRect.StopMovement();

        scrollRect.horizontalNormalizedPosition =
            Mathf.Clamp01(
                scrollRect.horizontalNormalizedPosition -
                movement / availableWidth
            );

        eventData.Use();
    }
}
