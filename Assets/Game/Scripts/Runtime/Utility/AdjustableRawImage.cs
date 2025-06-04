using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RawImage))]
public class AdjustableRawImage : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("References")]
    public Button toggleButton; // Button to toggle adjust mode
    public RectTransform resizeHandle; // Handle for resizing (assign a small UI element in corner)

    [Header("Settings")]
    public bool maintainAspectRatio = true;
    public float minWidth = 100f;
    public float minHeight = 100f;

    private RectTransform rectTransform;
    private Canvas canvas;
    private bool isAdjusting = false;
    private Vector2 originalLocalPointerPosition;
    private Vector3 originalPanelLocalPosition;
    private Vector2 originalSize;
    private bool isResizing = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleAdjustMode);
        }

        SetAdjustMode(false);
    }

    public void ToggleAdjustMode()
    {
        SetAdjustMode(!isAdjusting);
    }

    private void SetAdjustMode(bool active)
    {
        isAdjusting = active;
        if (resizeHandle != null) resizeHandle.gameObject.SetActive(active);
        RawImage rawImage = GetComponent<RawImage>();

        // Change appearance to indicate mode (optional)
        // RawImage rawImage = GetComponent<RawImage>();
        // if (rawImage != null)
        // {
        //     rawImage.color = active ? new Color(1, 1, 1, 0.8f) : Color.white;
        // }
    }

    public void OnPointerDown(PointerEventData data)
    {
        if (!isAdjusting) return;

        // Check if we're clicking on the resize handle
        if (RectTransformUtility.RectangleContainsScreenPoint(resizeHandle, data.position, data.pressEventCamera))
        {
            isResizing = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                data.position,
                data.pressEventCamera,
                out originalLocalPointerPosition);

            originalSize = rectTransform.sizeDelta;
        }
        else
        {
            // Prepare for moving
            isResizing = false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                data.position,
                data.pressEventCamera,
                out originalLocalPointerPosition);

            originalPanelLocalPosition = rectTransform.localPosition;
        }
    }

    public void OnDrag(PointerEventData data)
    {
        if (!isAdjusting) return;

        if (isResizing)
        {
            ResizeImage(data);
        }
        else
        {
            MoveImage(data);
        }
    }

    private void MoveImage(PointerEventData data)
    {
        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            data.position,
            data.pressEventCamera,
            out localPointerPosition))
        {
            Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;
            rectTransform.localPosition = originalPanelLocalPosition + offsetToOriginal;
        }
    }

    private void ResizeImage(PointerEventData data)
    {
        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            data.position,
            data.pressEventCamera,
            out localPointerPosition))
        {
            Vector2 offset = localPointerPosition - originalLocalPointerPosition;

            Vector2 newSize = originalSize + new Vector2(offset.x, -offset.y);

            // Apply minimum size constraints
            newSize.x = Mathf.Max(minWidth, newSize.x);
            newSize.y = Mathf.Max(minHeight, newSize.y);

            if (maintainAspectRatio)
            {
                float aspect = originalSize.x / originalSize.y;
                newSize.y = newSize.x / aspect;
            }

            rectTransform.sizeDelta = newSize;
        }
    }
}