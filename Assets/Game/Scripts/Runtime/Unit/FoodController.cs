using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FoodController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    FoodDataSO foodData;
    public float nutritionValue ;
    [SerializeField] private bool isRotten = false;
    [SerializeField] private Image foodImages;
    public bool IsBeingDragged { get; private set; }

    private RectTransform rectTransform;
    private Vector2 dragOffset;
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 
     private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    } 
    public void Initialize(FoodDataSO data)
    {    
        foodData = data;
        nutritionValue = foodData.nutritionValue;
        isRotten = false;

        UpdateFoodImage();
    }
    public void UpdateFoodImage()
    {
        if (isRotten)
        {
            foodImages.sprite = foodData.foodImgs.Length > 1 ? foodData.foodImgs[1] : foodData.foodImgs[0];
        }
        else
        {
            foodImages.sprite = foodData.foodImgs[0];
        }
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
        if (!ServiceLocator.Get<GameManager>().IsPositionInGameArea(rectTransform.anchoredPosition))
            ServiceLocator.Get<GameManager>().DespawnPools(gameObject);
    }
}
