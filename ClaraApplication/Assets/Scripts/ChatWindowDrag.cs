
using UnityEngine;
using UnityEngine.EventSystems;

public class ChatWindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private RectTransform chatWindow;

    private RectTransform dragPlane;
    private Vector3 pointerOffset;

    private void Awake()
    {
        if (chatWindow == null)
            chatWindow = transform.parent as RectTransform;

        dragPlane = chatWindow.parent as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            dragPlane,
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 pointerWorldPosition))
        {
            pointerOffset = chatWindow.position - pointerWorldPosition;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            dragPlane,
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 pointerWorldPosition))
        {
            chatWindow.position = pointerWorldPosition + pointerOffset;
        }
    }
}
