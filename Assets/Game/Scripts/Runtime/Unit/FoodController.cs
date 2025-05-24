using UnityEngine;
using UnityEngine.EventSystems;

public class FoodController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public float nutritionValue = 25f;
    public bool IsBeingDragged { get; private set; }

    private RectTransform rectTransform;
    private Vector2 dragOffset;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        GameManager.Instance.RegisterFood(this);
    }

    private void OnDestroy()
    {
        GameManager.Instance.UnregisterFood(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsBeingDragged = true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out dragOffset);
        dragOffset = rectTransform.anchoredPosition - dragOffset;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsBeingDragged)
        {
            Vector2 localPointerPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPointerPosition);
            rectTransform.anchoredPosition = localPointerPosition + dragOffset;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsBeingDragged = false;
        if (!GameManager.Instance.IsPositionInGameArea(rectTransform.anchoredPosition))
        {
            GameManager.Instance.DespawnFood(gameObject);
        }
    }
}
