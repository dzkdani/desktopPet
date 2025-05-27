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
    public Slider heightSlider; // Renamed from horizontalSlider
    public Slider monsterScaleSlider;
    public Slider uiScaleSlider;

    [Header("Slider Value Texts")]
    public TextMeshProUGUI widthValueText;
    public TextMeshProUGUI heightValueText; // Renamed from horizontalValueText
    public TextMeshProUGUI monsterValueText;
    public TextMeshProUGUI uiValueText;

    private void Start()
    {
        float maxWidth = Screen.currentResolution.width;
        float maxHeight = Screen.currentResolution.height;

        widthSlider.minValue = 100f;
        widthSlider.maxValue = maxWidth;
        heightSlider.minValue = 100f;
        heightSlider.maxValue = maxHeight;

        widthSlider.onValueChanged.AddListener(UpdateGameAreaWidth);
        heightSlider.onValueChanged.AddListener(UpdateGameAreaHeight); // Changed
        monsterScaleSlider.onValueChanged.AddListener(UpdateMonsterScale);
        uiScaleSlider.onValueChanged.AddListener(UpdateUIScale);



        // Optional: Set default slider values to match current Rect
        widthSlider.value = gameArea.sizeDelta.x;
        heightSlider.value = gameArea.sizeDelta.y;
        widthValueText.text = gameArea.sizeDelta.x.ToString("F0");
        heightValueText.text = gameArea.sizeDelta.y.ToString("F0");

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

        heightValueText.text = value.ToString("F0");
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
