using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI poopCounterText;
    public TextMeshProUGUI coinCounterText;
    public Button spawnPetButton;
    public Button spawnFoodButton;
    public Button gachaButton; // Assuming you have a Gacha button

    public TextMeshProUGUI messageText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        spawnPetButton.onClick.AddListener(() => GameManager.Instance.BuyMons());
        spawnFoodButton.onClick.AddListener(StartFoodPlacement);
        gachaButton.onClick.AddListener(() => GachaManager.Instance.RollGacha());
        UpdatePoopCounter();
        UpdateCoinCounter();
    }
    public void StartFoodPlacement()
    {
        GameManager.Instance.StartFoodPurchase(0);
    }


    public void UpdatePoopCounter()
    {
        poopCounterText.text = $"Poop : {GameManager.Instance.poopCollected}";
    }

    public void UpdateCoinCounter()
    {
        coinCounterText.text = $"Coin : {GameManager.Instance.coinCollected}";
    }
    public void ShowMessage(string message, float duration = 1f)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);
        CancelInvoke(nameof(HideMessage));
        Invoke(nameof(HideMessage), duration);
    }

    private void HideMessage()
    {
        messageText.gameObject.SetActive(false);
    }
}