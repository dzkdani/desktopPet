using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    [Header("UI Elements")]
    public TextMeshProUGUI poopCounterText;
    public TextMeshProUGUI coinCounterText;
    public Button spawnPetButton;
    public Button spawnFoodButton;
    public Button gachaButton; // Assuming you have a Gacha button

    public TextMeshProUGUI messageText;

    private void Awake()
    {
        ServiceLocator.Register(this);
    }
    
    void Start()
    {
        spawnPetButton.onClick.AddListener(() => ServiceLocator.Get<GameManager>().BuyMons());
        spawnFoodButton.onClick.AddListener(StartFoodPlacement);
        gachaButton.onClick.AddListener(() => ServiceLocator.Get<GachaManager>().RollGacha());
        UpdatePoopCounter();
        UpdateCoinCounter();
    }

    public void StartFoodPlacement()
    {
        ServiceLocator.Get<GameManager>().StartFoodPurchase(0);
    }

    public void UpdatePoopCounter()
    {
        poopCounterText.text = $"Poop : {ServiceLocator.Get<GameManager>().poopCollected}";
    }

    public void UpdateCoinCounter()
    {
        coinCounterText.text = $"Coin : {ServiceLocator.Get<GameManager>().coinCollected}";
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

    void OnDestroy()
    {
        // Unregister this instance from the ServiceLocator
        ServiceLocator.Unregister<UIManager>();
    }
}