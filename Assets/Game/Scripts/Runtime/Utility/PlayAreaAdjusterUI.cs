using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI controller for adjusting game area dimensions, position, monster scale, and UI scale through sliders
/// </summary>
public class PlayAreaAdjusterUI : MonoBehaviour
{
    #region References
    [Header("References")]
    public RectTransform gameArea;
    public CanvasScaler canvasScaler;
    public GameManager gameManager;
    #endregion

    #region Sliders
    [Header("Sliders")]
    public Slider widthSlider;
    public Slider heightSlider;
    public Slider horizontalPositionSlider;
    public Slider heightPositionSlider;
    public Slider monsterScaleSlider;
    public Slider uiScaleSlider;
    #endregion

    #region UI Text Elements
    [Header("Slider Value Texts")]
    public TextMeshProUGUI widthValueText;
    public TextMeshProUGUI heightValueText;
    public TextMeshProUGUI horizontalPositionValueText;
    public TextMeshProUGUI heightPositionValueText;
    public TextMeshProUGUI monsterValueText;
    public TextMeshProUGUI uiValueText;
    #endregion

    #region Constants
    private const float MIN_SIZE = 100f;
    private const float DEFAULT_MONSTER_SCALE = 1f;
    private const string DECIMAL_FORMAT = "F0";
    private const string SCALE_FORMAT = "F2";
    #endregion

    #region Private Fields
    private float maxScreenWidth;
    private float maxScreenHeight;
    private float initialGameAreaHeight;

    // Cache frequently accessed components
    private Vector2 cachedPosition;
    private Vector2 cachedSize;
    // private bool isDirty = false;
    #endregion

    #region Configuration
    [Header("Configuration")]
    // [SerializeField] private bool updateInRealTime = true;
    // [SerializeField] private float updateThreshold = 0.1f;
    #endregion

    #region Events
    public System.Action<Vector2> OnGameAreaSizeChanged;
    public System.Action<Vector2> OnGameAreaPositionChanged;
    public System.Action<float> OnMonsterScaleChanged;
    public System.Action<float> OnUIScaleChanged;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (!ValidateReferences()) return;
        
        CacheScreenValues();
        InitializeSliders();
        RegisterSliderCallbacks();
        SetInitialValues();
    }

    private void OnDestroy()
    {
        UnregisterSliderCallbacks();
    }
    #endregion

    #region Validation
    private bool ValidateReferences()
    {
        if (gameArea == null)
        {
            Debug.LogError($"[{nameof(PlayAreaAdjusterUI)}] GameArea reference is missing!");
            return false;
        }
        
        if (canvasScaler == null)
        {
            Debug.LogWarning($"[{nameof(PlayAreaAdjusterUI)}] CanvasScaler reference is missing!");
        }
        
        return true;
    }
    #endregion

    #region Initialization
    private void CacheScreenValues()
    {
        maxScreenWidth = Screen.currentResolution.width;
        maxScreenHeight = Screen.currentResolution.height;
        initialGameAreaHeight = gameArea != null ? gameArea.sizeDelta.y : 500f;
    }

    private void InitializeSliders()
    {
        if (widthSlider != null)
        {
            widthSlider.minValue = MIN_SIZE;
            widthSlider.maxValue = maxScreenWidth;
        }

        if (heightSlider != null)
        {
            heightSlider.minValue = MIN_SIZE;
            heightSlider.maxValue = initialGameAreaHeight;
        }

        if (horizontalPositionSlider != null)
        {
            horizontalPositionSlider.minValue = -maxScreenWidth / 2f;
            horizontalPositionSlider.maxValue = maxScreenWidth / 2f;
        }

        if (heightPositionSlider != null)
        {
            heightPositionSlider.minValue = -maxScreenHeight / 2f;
            heightPositionSlider.maxValue = maxScreenHeight / 2f;
        }
    }

    private void RegisterSliderCallbacks()
    {
        widthSlider?.onValueChanged.AddListener(UpdateGameAreaWidth);
        heightSlider?.onValueChanged.AddListener(UpdateGameAreaHeight);
        horizontalPositionSlider?.onValueChanged.AddListener(UpdateGameAreaHorizontalPosition);
        heightPositionSlider?.onValueChanged.AddListener(UpdateGameAreaVerticalPosition);
        monsterScaleSlider?.onValueChanged.AddListener(UpdateMonsterScale);
        uiScaleSlider?.onValueChanged.AddListener(UpdateUIScale);
    }

    private void UnregisterSliderCallbacks()
    {
        widthSlider?.onValueChanged.RemoveListener(UpdateGameAreaWidth);
        heightSlider?.onValueChanged.RemoveListener(UpdateGameAreaHeight);
        horizontalPositionSlider?.onValueChanged.RemoveListener(UpdateGameAreaHorizontalPosition);
        heightPositionSlider?.onValueChanged.RemoveListener(UpdateGameAreaVerticalPosition);
        monsterScaleSlider?.onValueChanged.RemoveListener(UpdateMonsterScale);
        uiScaleSlider?.onValueChanged.RemoveListener(UpdateUIScale);
    }

    private void SetInitialValues()
    {
        if (gameArea == null) return;

        // Store current position before changing anchor
        Vector2 currentPosition = gameArea.anchoredPosition;

        // Set anchor to bottom for vertical positioning
        gameArea.anchorMin = new Vector2(gameArea.anchorMin.x, 0f);
        gameArea.anchorMax = new Vector2(gameArea.anchorMax.x, 0f);
        gameArea.pivot = new Vector2(gameArea.pivot.x, 0f);

        // Restore the original position
        gameArea.anchoredPosition = currentPosition;

        // Set slider values
        if (widthSlider != null) widthSlider.value = gameArea.sizeDelta.x;
        if (heightSlider != null) heightSlider.value = gameArea.sizeDelta.y;
        if (horizontalPositionSlider != null) horizontalPositionSlider.value = gameArea.anchoredPosition.x;
        if (heightPositionSlider != null) heightPositionSlider.value = gameArea.anchoredPosition.y;
        if (monsterScaleSlider != null) monsterScaleSlider.value = DEFAULT_MONSTER_SCALE;
        if (uiScaleSlider != null && canvasScaler != null) uiScaleSlider.value = canvasScaler.scaleFactor;

        // Update text displays
        UpdateValueText(widthValueText, gameArea.sizeDelta.x, DECIMAL_FORMAT);
        UpdateValueText(heightValueText, gameArea.sizeDelta.y, DECIMAL_FORMAT);
        UpdateValueText(horizontalPositionValueText, gameArea.anchoredPosition.x, DECIMAL_FORMAT);
        UpdateValueText(heightPositionValueText, gameArea.anchoredPosition.y, DECIMAL_FORMAT);
    }
    #endregion

    #region Game Area Size Updates
    public void UpdateGameAreaWidth(float value)
    {
        if (gameArea == null) return;

        value = Mathf.Clamp(value, MIN_SIZE, maxScreenWidth);
        Vector2 size = gameArea.sizeDelta;
        size.x = value;
        gameArea.sizeDelta = size;

        UpdateValueText(widthValueText, value, DECIMAL_FORMAT);
    }

    public void UpdateGameAreaHeight(float value)
    {
        if (gameArea == null) return;

        value = Mathf.Clamp(value, MIN_SIZE, initialGameAreaHeight);
        
        // Store current position before changing anchor (if not already set)
        Vector2 currentPosition = gameArea.anchoredPosition;
        
        // Set anchor to bottom
        gameArea.anchorMin = new Vector2(gameArea.anchorMin.x, 0f);
        gameArea.anchorMax = new Vector2(gameArea.anchorMax.x, 0f);
        gameArea.pivot = new Vector2(gameArea.pivot.x, 0f);
        
        // Restore position and set new size
        gameArea.anchoredPosition = currentPosition;
        Vector2 size = gameArea.sizeDelta;
        size.y = value;
        gameArea.sizeDelta = size;

        UpdateValueText(heightValueText, value, DECIMAL_FORMAT);
    }
    #endregion

    #region Game Area Position Updates
    public void UpdateGameAreaHorizontalPosition(float value)
    {
        UpdateGameAreaPosition(value, true);
    }

    public void UpdateGameAreaVerticalPosition(float value)
    {
        UpdateGameAreaPosition(value, false);
    }

    private void UpdateGameAreaPosition(float value, bool isHorizontal)
    {
        if (gameArea == null) return;

        float clampedValue = isHorizontal 
            ? Mathf.Clamp(value, -maxScreenWidth / 2f, maxScreenWidth / 2f)
            : Mathf.Clamp(value, -maxScreenHeight / 2f, maxScreenHeight / 2f);

        cachedPosition = gameArea.anchoredPosition;
        
        if (isHorizontal)
            cachedPosition.x = clampedValue;
        else
            cachedPosition.y = clampedValue;
            
        gameArea.anchoredPosition = cachedPosition;

        var textComponent = isHorizontal ? horizontalPositionValueText : heightPositionValueText;
        UpdateValueText(textComponent, clampedValue, DECIMAL_FORMAT);
    }
    #endregion

    #region Scale Updates
    public void UpdateMonsterScale(float value)
    {
        if (gameManager?.activeMonsters == null) return;

        // Use a more efficient iteration
        var monsters = gameManager.activeMonsters;
        var scaleVector = Vector3.one * value;
        
        for (int i = 0; i < monsters.Count; i++)
        {
            if (monsters[i] != null)
                monsters[i].transform.localScale = scaleVector;
        }
        
        UpdateValueText(monsterValueText, value, SCALE_FORMAT);
    }

    public void UpdateUIScale(float value)
    {
        if (canvasScaler != null)
            canvasScaler.scaleFactor = value;
        
        UpdateValueText(uiValueText, value, SCALE_FORMAT);
    }
    #endregion

    #region Utility Methods
    private void UpdateValueText(TextMeshProUGUI textComponent, float value, string format)
    {
        if (textComponent != null)
            textComponent.text = value.ToString(format);
    }
    #endregion
}
