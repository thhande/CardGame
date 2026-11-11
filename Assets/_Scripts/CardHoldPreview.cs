using System;
using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class CardHoldPreview : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private const float HoldSeconds = 0.4f;
    private PointerEventData press;
    private Vector2 pressPosition;
    private float previewTime;
    private bool showing;

    public Func<bool> Show { private get; set; }
    public Action Hide { private get; set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (press != null || eventData.button != PointerEventData.InputButton.Left) return;
        press = eventData;
        pressPosition = eventData.position;
        previewTime = Time.unscaledTime + HoldSeconds;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData == press) Cancel();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData == press) Cancel();
    }

    private void Update()
    {
        if (press == null) return;
        EventSystem eventSystem = EventSystem.current;
        if (press.dragging || (eventSystem != null && (press.position - pressPosition).sqrMagnitude >
            eventSystem.pixelDragThreshold * eventSystem.pixelDragThreshold))
        {
            Cancel();
            return;
        }

        if (showing || Time.unscaledTime < previewTime) return;
        showing = Show?.Invoke() == true;
        if (showing) press.eligibleForClick = false;
        else Cancel();
    }

    private void OnDisable() => Cancel();

    public void Cancel()
    {
        press = null;
        if (showing) Hide?.Invoke();
        showing = false;
    }
}
