using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class MiniGameInteraction : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    public Camera miniGameCamera;
    public LayerMask miniGameLayer;

    private RawImage _rawImage;
    private bool _isDragging = false;
    private GameObject _currentSelected;
    private PointerEventData _currentPointerData;

    private void Awake()
    {
        _rawImage = GetComponent<RawImage>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(_rawImage.rectTransform, eventData.position, eventData.pressEventCamera))
            return;

        Ray ray = GetMiniGameRay(eventData);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, miniGameLayer))
        {
            _currentSelected = hit.collider.gameObject;
            _currentPointerData = eventData;

            // Create proper PointerEventData for the 3D object
            var pointerData = new PointerEventData(EventSystem.current)
            {
                pointerId = eventData.pointerId,
                position = eventData.position,
                button = eventData.button,
                pointerPressRaycast = new RaycastResult
                {
                    gameObject = _currentSelected,
                    worldPosition = hit.point,
                    worldNormal = hit.normal,
                    screenPosition = eventData.position,
                    distance = hit.distance,
                    index = 0,
                    depth = 0,
                    sortingLayer = 0,
                    sortingOrder = 0
                }
            };

            ExecuteEvents.Execute(_currentSelected, pointerData, ExecuteEvents.pointerDownHandler);
            _isDragging = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_currentSelected != null)
        {
            // Use the stored pointer data or create new one
            var pointerData = _currentPointerData ?? new PointerEventData(EventSystem.current)
            {
                pointerId = eventData.pointerId,
                position = eventData.position,
                button = eventData.button
            };

            ExecuteEvents.Execute(_currentSelected, pointerData, ExecuteEvents.pointerUpHandler);

            if (!_isDragging)
            {
                ExecuteEvents.Execute(_currentSelected, pointerData, ExecuteEvents.pointerClickHandler);
            }
        }

        _isDragging = false;
        _currentSelected = null;
        _currentPointerData = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Handled in OnPointerUp
    }

    private Ray GetMiniGameRay(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rawImage.rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);

        Rect rect = _rawImage.rectTransform.rect;
        Vector2 uv = new Vector2(
            (localPoint.x + rect.width * 0.5f) / rect.width,
            (localPoint.y + rect.height * 0.5f) / rect.height);

        return miniGameCamera.ViewportPointToRay(uv);
    }

    private void Update()
    {
        if (_isDragging && _currentSelected != null && _currentPointerData != null)
        {
            Ray ray = GetMiniGameRay(_currentPointerData);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, miniGameLayer))
            {
                // Update the pointer data for dragging
                _currentPointerData.pointerCurrentRaycast = new RaycastResult
                {
                    gameObject = _currentSelected,
                    worldPosition = hit.point,
                    worldNormal = hit.normal,
                    screenPosition = _currentPointerData.position,
                    distance = hit.distance
                };

                ExecuteEvents.Execute(_currentSelected, _currentPointerData, ExecuteEvents.dragHandler);
            }
        }
    }
}