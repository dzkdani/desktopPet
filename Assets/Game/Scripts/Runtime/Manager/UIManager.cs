using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI poopCounterText;
    public Button spawnPetButton;
    public Button spawnFoodButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        spawnPetButton.onClick.AddListener(GameManager.Instance.BuyPet);
        spawnFoodButton.onClick.AddListener(GameManager.Instance.SpawnFood);
        UpdatePoopCounter();
    }

    public void UpdatePoopCounter()
    {
        poopCounterText.text = $"Poop Collected: {GameManager.Instance.poopCollected}";
    }

}