
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BackgroundThumbnail : MonoBehaviour,
    IPointerClickHandler,
    IInitializePotentialDragHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image preview;
    [SerializeField] private Outline selectionOutline;
    [SerializeField] private ScrollRect scrollRect;

    private Action onSelected;
    private bool wasDragged;

    public void Setup(
        Sprite sprite,
        Action clickAction,
        ScrollRect parentScrollRect)
    {
        preview.sprite = sprite;
        preview.preserveAspect = true;

        onSelected = clickAction;
        scrollRect = parentScrollRect;
        wasDragged = false;

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectionOutline != null)
            selectionOutline.enabled = selected;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // A drag must never select a background.
        if (wasDragged || eventData.dragging)
            return;

        onSelected?.Invoke();
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        wasDragged = false;
        scrollRect.OnInitializePotentialDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        wasDragged = true;
        scrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        scrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        scrollRect.OnEndDrag(eventData);
    }
}
