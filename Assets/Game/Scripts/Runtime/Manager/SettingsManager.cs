using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Comprehensive setup manager handling game area, audio, and language settings
/// </summary>
public class SettingsManager : MonoBehaviour
{
    [Header("Game Area References")]
    public RectTransform gameArea;
    public CanvasScaler canvasScaler;
    private GameManager gameManager;

    [Header("Game Area Sliders")]
    public Slider widthSlider;
    public Slider heightSlider;
    public Slider horizontalPositionSlider;
    public Slider verticalPositionSlider;
    public Slider monsterScaleSlider;
    public Slider uiScaleSlider;
    [Header("Input Field References")]
    [SerializeField] private TMP_InputField widthInputField;
    [SerializeField] private TMP_InputField heightInputField;
    [SerializeField] private TMP_InputField horizontalPositionInputField;
    [SerializeField] private TMP_InputField verticalPositionInputField;
    [SerializeField] private TMP_InputField monsterScaleInputField;
    [SerializeField] private TMP_InputField uiScaleInputField;

    [Header("Game Area Texts")]
    public TextMeshProUGUI widthValueText;
    public TextMeshProUGUI heightValueText;
    public TextMeshProUGUI horizontalPositionValueText;
    public TextMeshProUGUI verticalPositionValueText;
    public TextMeshProUGUI monsterValueText;
    public TextMeshProUGUI uiValueText;
    [Header("Panel References")]
    public GameObject gameAreaPanel;
    public GameObject audioLanguagePanel;

    [Header("Language Settings")]
    public TMP_Dropdown languageDropdown;
    public List<SystemLanguage> supportedLanguages = new List<SystemLanguage>
    {
        SystemLanguage.English,
        SystemLanguage.French,
        SystemLanguage.Spanish
    };

    private const float MIN_SIZE = 100f;
    private const float DEFAULT_MONSTER_SCALE = 1f;
    private const string DECIMAL_FORMAT = "F0";
    private const string SCALE_FORMAT = "F2";
    private const string VOLUME_FORMAT = "F1";
    private const float MIN_VOLUME_DB = -80f;
    private const float MAX_VOLUME_DB = 0f;

    private float maxScreenWidth;
    private float maxScreenHeight;
    private float initialGameAreaHeight;
    private Vector2 cachedPosition;
    private void Awake()
    {
        ServiceLocator.Register(this);
    }
    private void Start()
    {
        gameManager = ServiceLocator.Get<GameManager>();
        InitializeGameAreaSettings();
        InitializeLanguageSettings();
    }

    private void OnDestroy()
    {
        UnregisterAllCallbacks();
        ServiceLocator.Unregister<SettingsManager>();    }


    private void InitializeGameAreaSettings()
    {
        if (!ValidateGameAreaReferences()) return;

        CacheScreenValues();
        InitializeGameAreaSliders();
        InitializeInputFields();
        SetInitialGameAreaValues();
    }



    private void InitializeLanguageSettings()
    {
        if (languageDropdown == null) return;

        // Populate dropdown with supported languages
        languageDropdown.ClearOptions();
        List<string> languageOptions = new List<string>();

        foreach (var lang in supportedLanguages)
        {
            languageOptions.Add(lang.ToString());
        }

        languageDropdown.AddOptions(languageOptions);

        // Set current language from preferences
        int currentLangIndex = PlayerPrefs.GetInt("Language", 0);
        currentLangIndex = Mathf.Clamp(currentLangIndex, 0, supportedLanguages.Count - 1);
        languageDropdown.value = currentLangIndex;

        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
    }

    private bool ValidateGameAreaReferences()
    {
        if (gameArea == null) return false;
        if (canvasScaler == null) return false;
        return true;
    }

    private void CacheScreenValues()
    {
        maxScreenWidth = Screen.currentResolution.width;
        maxScreenHeight = Screen.currentResolution.height;
        initialGameAreaHeight = gameArea.sizeDelta.y;
    }

    private void InitializeGameAreaSliders()
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

        if (verticalPositionSlider != null)
        {
            verticalPositionSlider.minValue = -maxScreenHeight / 2f;
            verticalPositionSlider.maxValue = maxScreenHeight / 2f;
        }

        RegisterGameAreaCallbacks();
    }
    private void InitializeInputFields()
    {
        // Set initial values
        if (widthInputField != null)
        {
            widthInputField.text = gameArea.sizeDelta.x.ToString(DECIMAL_FORMAT);
            widthInputField.onEndEdit.AddListener(OnWidthInputChanged);
        }

        if (heightInputField != null)
        {
            heightInputField.text = gameArea.sizeDelta.y.ToString(DECIMAL_FORMAT);
            heightInputField.onEndEdit.AddListener(OnHeightInputChanged);
        }

        if (horizontalPositionInputField != null)
        {
            horizontalPositionInputField.text = gameArea.anchoredPosition.x.ToString(DECIMAL_FORMAT);
            horizontalPositionInputField.onEndEdit.AddListener(OnHorizontalPosInputChanged);
        }

        if (verticalPositionInputField != null)
        {
            verticalPositionInputField.text = gameArea.anchoredPosition.y.ToString(DECIMAL_FORMAT);
            verticalPositionInputField.onEndEdit.AddListener(OnVerticalPosInputChanged);
        }

        if (monsterScaleInputField != null)
        {
            monsterScaleInputField.text = DEFAULT_MONSTER_SCALE.ToString(SCALE_FORMAT);
            monsterScaleInputField.onEndEdit.AddListener(OnMonsterScaleInputChanged);
        }

        if (uiScaleInputField != null)
        {
            uiScaleInputField.text = canvasScaler.scaleFactor.ToString(SCALE_FORMAT);
            uiScaleInputField.onEndEdit.AddListener(OnUIScaleInputChanged);
        }
        // Register input field callbacks
        RegisterInputFieldCallbacks();
    }

    private void SetInitialGameAreaValues()
    {
        if (gameArea == null) return;
        if (widthSlider != null) widthSlider.value = gameArea.sizeDelta.x;
        if (heightSlider != null) heightSlider.value = gameArea.sizeDelta.y;
        if (horizontalPositionSlider != null) horizontalPositionSlider.value = gameArea.anchoredPosition.x;
        if (verticalPositionSlider != null) verticalPositionSlider.value = gameArea.anchoredPosition.y;
        if (monsterScaleSlider != null) monsterScaleSlider.value = DEFAULT_MONSTER_SCALE;
        if (uiScaleSlider != null && canvasScaler != null) uiScaleSlider.value = canvasScaler.scaleFactor;

        // Update text displays
        UpdateValueText(widthValueText, gameArea.sizeDelta.x, DECIMAL_FORMAT);
        UpdateValueText(heightValueText, gameArea.sizeDelta.y, DECIMAL_FORMAT);
        UpdateValueText(horizontalPositionValueText, gameArea.anchoredPosition.x, DECIMAL_FORMAT);        UpdateValueText(verticalPositionValueText, gameArea.anchoredPosition.y, DECIMAL_FORMAT);
    }
    #region Input Field Handlers
    private void OnWidthInputChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            result = Mathf.Clamp(result, MIN_SIZE, maxScreenWidth);
            widthSlider.value = result;
            UpdateGameAreaWidth(result);
        }
        else
        {
            widthInputField.text = gameArea.sizeDelta.x.ToString(DECIMAL_FORMAT);
        }
    }

    private void OnHeightInputChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            result = Mathf.Clamp(result, MIN_SIZE, initialGameAreaHeight);
            heightSlider.value = result;
            UpdateGameAreaHeight(result);
        }
        else
        {
            heightInputField.text = gameArea.sizeDelta.y.ToString(DECIMAL_FORMAT);
        }
    }

    private void OnHorizontalPosInputChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            result = Mathf.Clamp(result, -maxScreenWidth / 2f, maxScreenWidth / 2f);
            horizontalPositionSlider.value = result;
            UpdateGameAreaHorizontalPosition(result);
        }
        else
        {
            horizontalPositionInputField.text = gameArea.anchoredPosition.x.ToString(DECIMAL_FORMAT);
        }
    }

    private void OnVerticalPosInputChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            result = Mathf.Clamp(result, -maxScreenHeight / 2f, maxScreenHeight / 2f);
            verticalPositionSlider.value = result;
            UpdateGameAreaVerticalPosition(result);
        }
        else
        {
            verticalPositionInputField.text = gameArea.anchoredPosition.y.ToString(DECIMAL_FORMAT);
        }
    }

    private void OnMonsterScaleInputChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            monsterScaleSlider.value = result;
            UpdateMonsterScale(result);
        }
        else
        {
            monsterScaleInputField.text = DEFAULT_MONSTER_SCALE.ToString(SCALE_FORMAT);
        }
    }

    private void OnUIScaleInputChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            uiScaleSlider.value = result;
            UpdateUIScale(result);
        }
        else
        {
            uiScaleInputField.text = canvasScaler.scaleFactor.ToString(SCALE_FORMAT);
        }
    }
    #endregion

    #region Callback Registration
    private void RegisterGameAreaCallbacks()
    {
        widthSlider?.onValueChanged.AddListener(UpdateGameAreaWidth);
        heightSlider?.onValueChanged.AddListener(UpdateGameAreaHeight);
        horizontalPositionSlider?.onValueChanged.AddListener(UpdateGameAreaHorizontalPosition);
        verticalPositionSlider?.onValueChanged.AddListener(UpdateGameAreaVerticalPosition);
        monsterScaleSlider?.onValueChanged.AddListener(UpdateMonsterScale);
        uiScaleSlider?.onValueChanged.AddListener(UpdateUIScale);
    }
    private void RegisterInputFieldCallbacks()
    {
        if (widthInputField != null) widthInputField.onEndEdit.AddListener(OnWidthInputChanged);
        if (heightInputField != null) heightInputField.onEndEdit.AddListener(OnHeightInputChanged);
        if (horizontalPositionInputField != null) horizontalPositionInputField.onEndEdit.AddListener(OnHorizontalPosInputChanged);
        if (verticalPositionInputField != null) verticalPositionInputField.onEndEdit.AddListener(OnVerticalPosInputChanged);
        if (monsterScaleInputField != null) monsterScaleInputField.onEndEdit.AddListener(OnMonsterScaleInputChanged);
        if (uiScaleInputField != null) uiScaleInputField.onEndEdit.AddListener(OnUIScaleInputChanged);
    }


    private void UnregisterAllCallbacks()
    {
        // Game Area
        widthSlider?.onValueChanged.RemoveListener(UpdateGameAreaWidth);
        heightSlider?.onValueChanged.RemoveListener(UpdateGameAreaHeight);
        horizontalPositionSlider?.onValueChanged.RemoveListener(UpdateGameAreaHorizontalPosition);
        verticalPositionSlider?.onValueChanged.RemoveListener(UpdateGameAreaVerticalPosition);
        monsterScaleSlider?.onValueChanged.RemoveListener(UpdateMonsterScale);
        uiScaleSlider?.onValueChanged.RemoveListener(UpdateUIScale);
        // Input Fields
        widthInputField?.onEndEdit.RemoveListener(OnWidthInputChanged);
        heightInputField?.onEndEdit.RemoveListener(OnHeightInputChanged);
        horizontalPositionInputField?.onEndEdit.RemoveListener(OnHorizontalPosInputChanged);
        verticalPositionInputField?.onEndEdit.RemoveListener(OnVerticalPosInputChanged);
        monsterScaleInputField?.onEndEdit.RemoveListener(OnMonsterScaleInputChanged);
        uiScaleInputField?.onEndEdit.RemoveListener(OnUIScaleInputChanged);

        // Language
        languageDropdown?.onValueChanged.RemoveListener(OnLanguageChanged);
    }
    #endregion

    #region Update Methods (modified to sync with input fields)
    public void UpdateGameAreaWidth(float value)
    {
        if (gameArea == null) return;

        value = Mathf.Clamp(value, MIN_SIZE, maxScreenWidth);
        Vector2 size = gameArea.sizeDelta;
        size.x = value;
        gameArea.sizeDelta = size;

        UpdateValueText(widthValueText, value, DECIMAL_FORMAT);
        if (widthInputField != null) widthInputField.text = value.ToString(DECIMAL_FORMAT);
    }

    public void UpdateGameAreaHeight(float value)
    {
        if (gameArea == null) return;

        value = Mathf.Clamp(value, MIN_SIZE, initialGameAreaHeight);
        Vector2 size = gameArea.sizeDelta;
        size.y = value;
        gameArea.sizeDelta = size;

        UpdateValueText(heightValueText, value, DECIMAL_FORMAT);
        if (heightInputField != null) heightInputField.text = value.ToString(DECIMAL_FORMAT);
    }

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
        {
            cachedPosition.x = clampedValue;
            if (horizontalPositionInputField != null) horizontalPositionInputField.text = clampedValue.ToString(DECIMAL_FORMAT);
        }
        else
        {
            cachedPosition.y = clampedValue;
            if (verticalPositionInputField != null) verticalPositionInputField.text = clampedValue.ToString(DECIMAL_FORMAT);
        }

        gameArea.anchoredPosition = cachedPosition;

        var textComponent = isHorizontal ? horizontalPositionValueText : verticalPositionValueText;
        UpdateValueText(textComponent, clampedValue, DECIMAL_FORMAT);
    }

    public void UpdateMonsterScale(float value)
    {
        if (gameManager?.activeMonsters == null) return;

        var monsters = gameManager.activeMonsters;
        var scaleVector = Vector3.one * value;

        for (int i = 0; i < monsters.Count; i++)
        {
            if (monsters[i] != null)
                monsters[i].transform.localScale = scaleVector;
        }

        UpdateValueText(monsterValueText, value, SCALE_FORMAT);
        if (monsterScaleInputField != null) monsterScaleInputField.text = value.ToString(SCALE_FORMAT);
    }

    public void UpdateUIScale(float value)
    {
        if (canvasScaler != null)
            canvasScaler.scaleFactor = value;

        UpdateValueText(uiValueText, value, SCALE_FORMAT);
        if (uiScaleInputField != null) uiScaleInputField.text = value.ToString(SCALE_FORMAT);
    }
    #endregion

    #region Language Controls
    private void OnLanguageChanged(int index)
    {
        if (index < 0 || index >= supportedLanguages.Count) return;

        SystemLanguage selectedLanguage = supportedLanguages[index];
        PlayerPrefs.SetInt("Language", index);

        // Implement your language change logic here
        // e.g., LocalizationManager.Instance.SetLanguage(selectedLanguage);

        Debug.Log($"Language changed to: {selectedLanguage}");
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