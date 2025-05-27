using UnityEngine;
using UnityEngine.UI;
using TMPro; // Optional if using TextMeshPro

public class PlayAreaAdjusterUI : MonoBehaviour
{
    [Header("References")]
    public RectTransform gameArea;
    public CanvasScaler canvasScaler;
    public GameManager gameManager; // To access monsters

    [Header("Sliders")]
    public Slider widthSlider;
    public Slider horizontalPositionSlider; // Renamed from horizontalSlider
    public Slider heightPositionSlider; // Renamed from horizontalSlider
    public Slider monsterScaleSlider;
    public Slider uiScaleSlider;

    [Header("Slider Value Texts")]
    public TextMeshProUGUI widthValueText;
    public TextMeshProUGUI horizontalPositionValueText; // Renamed from horizontalValueText
    public TextMeshProUGUI heightPositionValueText; // Renamed from horizontalValueText
    public TextMeshProUGUI monsterValueText;
    public TextMeshProUGUI uiValueText;

    private void Start()
    {
        float maxWidth = Screen.currentResolution.width;
        float maxHeight = Screen.currentResolution.height;

        widthSlider.minValue = 100f;
        widthSlider.maxValue = maxWidth;
        horizontalPositionSlider.minValue = -maxWidth / 2f;
        horizontalPositionSlider.maxValue = maxWidth / 2f;
        heightPositionSlider.minValue = -maxHeight / 2f;
        heightPositionSlider.maxValue = maxHeight / 2f;

        widthSlider.onValueChanged.AddListener(UpdateGameAreaWidth);
        horizontalPositionSlider.onValueChanged.AddListener(UpdateGameAreaHorizontalPosition); // Changed
        heightPositionSlider.onValueChanged.AddListener(UpdateGameAreaVerticalPosition); // Changed
        monsterScaleSlider.onValueChanged.AddListener(UpdateMonsterScale);
        uiScaleSlider.onValueChanged.AddListener(UpdateUIScale);



        // Optional: Set default slider values to match current Rect
        widthSlider.value = gameArea.sizeDelta.x;
        horizontalPositionSlider.value = gameArea.anchoredPosition.x;
        heightPositionSlider.value = gameArea.anchoredPosition.y;
        widthValueText.text = gameArea.sizeDelta.x.ToString("F0");
        heightPositionValueText.text = gameArea.anchoredPosition.y.ToString("F0");
        horizontalPositionValueText.text = gameArea.anchoredPosition.x.ToString("F0");

        monsterScaleSlider.value = 1f;
        uiScaleSlider.value = canvasScaler.scaleFactor;
    }

    public void UpdateGameAreaHeight(float value)
    {
        float maxHeight = Screen.currentResolution.height;
        value = Mathf.Clamp(value, 100f, maxHeight);

        Vector2 size = gameArea.sizeDelta;
        size.y = value;
        gameArea.sizeDelta = size;

        heightPositionValueText.text = value.ToString("F0");
    }

    public void UpdateGameAreaWidth(float value)
    {
        float maxWidth = Screen.currentResolution.width;

        value = Mathf.Clamp(value, 100f, maxWidth); // Prevent too small/large
        Vector2 size = gameArea.sizeDelta;
        size.x = value;
        gameArea.sizeDelta = size;

        widthValueText.text = value.ToString("F0");
    }
    public void UpdateGameAreaHorizontalPosition(float value)
    {
        float maxWidth = Screen.currentResolution.width;
        value = Mathf.Clamp(value, -maxWidth / 2f, maxWidth / 2f);

        Vector2 anchoredPosition = gameArea.anchoredPosition;
        anchoredPosition.x = value;
        gameArea.anchoredPosition = anchoredPosition;

        horizontalPositionValueText.text = value.ToString("F0");
    }
    public void UpdateGameAreaVerticalPosition(float value)
    {
        float maxHeight = Screen.currentResolution.height;
        value = Mathf.Clamp(value, -maxHeight / 2f, maxHeight / 2f);

        Vector2 anchoredPosition = gameArea.anchoredPosition;
        anchoredPosition.y = value;
        gameArea.anchoredPosition = anchoredPosition;

        heightPositionValueText.text = value.ToString("F0");
    }


    public void UpdateMonsterScale(float value)
    {
        foreach (var monster in gameManager.activeMonsters)
        {
            monster.transform.localScale = Vector3.one * value;
        }
        monsterValueText.text = value.ToString("F2");
    }

    public void UpdateUIScale(float value)
    {
        canvasScaler.scaleFactor = value;
        uiValueText.text = value.ToString("F2");
    }
}
