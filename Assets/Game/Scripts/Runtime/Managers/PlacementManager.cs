using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class PlacementManager : MonoBehaviour
{
    private GameObject currentPreview;
    private RectTransform gameArea;
    private RectTransform previewRect;

    private System.Action<Vector2> onConfirmPlace;
    private System.Action onCancel;

    [Header("Visual Feedback")]
    [SerializeField] private Color validColor = Color.white;
    [SerializeField] private Color invalidColor = Color.red;

    private bool allowMultiplePlacement = false;
    private bool isPlacingMedicine = false;
    [Header("Placement Prefabs")]
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private GameObject medicinePrefab;
    private Transform prefabParent;

    [Header("Placement Canvas")]
    [SerializeField] private RectTransform canvasParent;
    public RectTransform GetCanvasParent() => canvasParent;

    public GameObject GetPrefabForItemType(ItemType type)
    {
        return type switch
        {
            ItemType.Food => foodPrefab,
            ItemType.Medicine => medicinePrefab,
            _ => null
        };
    }

    private void Awake()
    {
        ServiceLocator.Register(this);
        prefabParent = foodPrefab.transform.parent;
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<PlacementManager>();
    }

    public void StartPlacement(
        GameObject prefab,
        RectTransform area,
        System.Action<Vector2> onPlace,
        System.Action onCancel = null,
        bool allowMultiple = false,
        Sprite previewSprite = null,
        bool isMedicine = false)
    {
        ResetPlacement();
        gameArea = area;
        currentPreview = prefab;
        currentPreview.SetActive(true);
        currentPreview.transform.SetParent(area);
        previewRect = currentPreview.GetComponent<RectTransform>();

        this.onConfirmPlace = onPlace;
        this.onCancel = onCancel;
        this.allowMultiplePlacement = allowMultiple;
        this.isPlacingMedicine = isMedicine;

        // Set preview sprite if provided
        if (previewSprite != null && currentPreview.TryGetComponent<Image>(out var image))
        {
            Debug.Log($"Setting preview sprite: {previewSprite.name}");
            image.sprite = previewSprite;
        }
    }

    private void Update()
    {
        if (currentPreview == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(gameArea, Input.mousePosition, null, out var pos);
        previewRect.anchoredPosition = pos;

        bool isValid = isPlacingMedicine ? IsOverSickMonster() : IsInsideGameArea(pos);
        UpdatePreviewVisual(isValid);

        if (Input.GetMouseButtonDown(0))
        {
            if (isValid)
            {
                onConfirmPlace?.Invoke(pos);

                if (!allowMultiplePlacement)
                {
                    CancelPlacement();
                }
            }
            else
            {
                string msg = isPlacingMedicine
                    ? "Only usable on monsters!"
                    : "Can't place outside the game area!";
                ServiceLocator.Get<UIManager>()?.ShowMessage(msg, 1f);
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
        }
    }

    public void CancelPlacement()
    {
        ResetPlacement();
        onCancel?.Invoke();
    }
    public void ResetPlacement()
    {
        if (currentPreview != null)
        {
            currentPreview.SetActive(false);
            currentPreview.transform.SetParent(prefabParent);
            currentPreview = null;
        }
    }

    private bool IsInsideGameArea(Vector2 localPos)
    {
        if (gameArea == null) return false;
        Rect rect = gameArea.rect;
        return rect.Contains(localPos);
    }

    private bool IsOverSickMonster()
    {
        return TryGetMonsterUnderCursor() != null;
    }

    private void UpdatePreviewVisual(bool isValid)
    {
        if (currentPreview.TryGetComponent<Image>(out var img))
        {
            img.color = isValid ? validColor : invalidColor;
        }
    }
    public MonsterController TryGetMonsterUnderCursor()
    {
        PointerEventData pointer = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, raycastResults);

        foreach (var result in raycastResults)
        {
            MonsterController monster = result.gameObject.GetComponentInParent<MonsterController>();
            if (monster != null)
            {
                return monster;
            }
        }

        return null;
    }
}
