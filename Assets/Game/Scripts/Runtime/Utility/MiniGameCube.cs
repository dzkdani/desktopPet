using UnityEngine;
using UnityEngine.EventSystems;

public class MiniGameCube : MonoBehaviour,
    IPointerClickHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    [Header("Visual Feedback")]
    public Color hoverColor = Color.yellow;
    public Color clickColor = Color.red;

    private Material _material;
    private Color _originalColor;

    private void Awake()
    {
        _material = GetComponent<Renderer>().material;
        _originalColor = _material.color;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Cube clicked at: " + eventData.pointerCurrentRaycast.worldPosition);
        // Your click logic here
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _material.color = clickColor;
        Debug.Log("Pointer down on cube");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _material.color = _originalColor;
        Debug.Log("Pointer up from cube");
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Example dragging implementation
        if (eventData.pointerCurrentRaycast.isValid)
        {
            transform.position = eventData.pointerCurrentRaycast.worldPosition;
        }
    }
}