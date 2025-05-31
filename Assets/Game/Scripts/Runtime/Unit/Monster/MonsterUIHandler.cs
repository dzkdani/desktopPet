using UnityEngine;
using TMPro;

[System.Serializable]
public class MonsterUIHandler
{
    [Header("UI Elements")]
    public GameObject hungerInfo;
    public GameObject happinessInfo;
    [SerializeField] private TextMeshProUGUI hungerText;
    [SerializeField] private TextMeshProUGUI happinessText;
    
    // Cached components
    private CanvasGroup _hungerInfoCg;
    private CanvasGroup _happinessInfoCg;
    
    public void Init()
    {
        _hungerInfoCg = hungerInfo.GetComponent<CanvasGroup>();
        _happinessInfoCg = happinessInfo.GetComponent<CanvasGroup>();
        
        // Start hidden
        _hungerInfoCg.alpha = 0f;
        _happinessInfoCg.alpha = 0f;
    }
    
    public void UpdateHungerDisplay(float hunger, bool showUI)
    {
        // Always update the text content (color stays as set in inspector)
        hungerText.text = $"Hunger: {hunger:F1}%";
        
        // Control visibility based on hover
        _hungerInfoCg.alpha = showUI ? 1f : 0f;
    }
    
    public void UpdateHappinessDisplay(float happiness, bool showUI)
    {
        // Always update the text content (color stays as set in inspector)
        happinessText.text = $"Happiness: {happiness:F1}%";
        
        // Control visibility based on hover
        _happinessInfoCg.alpha = showUI ? 1f : 0f;
    }
}