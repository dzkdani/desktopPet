using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Button UIMenuButton;
    public GameObject UIMenuPanel;
    public CanvasGroup UIMenuCanvasGroup;
    public TextMeshProUGUI poopCounterText;
    public TextMeshProUGUI coinCounterText;
    public Button spawnPetButton;
    public Button spawnFoodButton;
    public Button gachaButton; 
    public TextMeshProUGUI messageText;

    private bool _onMenuOpened = false;
    private bool _isAnimating = false;
    private Vector3 _menuInitialPosition; // Store initial position
    // TEMPORARY FEATURE: Button scaling animation
    private Vector3 _buttonInitialScale;
    private Vector3 _buttonInitialPosition;

    private void Awake()
    {
        ServiceLocator.Register(this);
        _onMenuOpened = false;
        UIMenuPanel.SetActive(_onMenuOpened);
        
        // Store the initial position of the menu
        _menuInitialPosition = UIMenuPanel.GetComponent<RectTransform>().anchoredPosition;
        
        // TEMPORARY FEATURE: Store initial button scale and position
        _buttonInitialScale = UIMenuButton.transform.localScale;
        _buttonInitialPosition = UIMenuButton.GetComponent<RectTransform>().anchoredPosition;
    }
    
    void Start()
    {
        UIMenuButton.onClick.AddListener(ShowMenu);
        spawnPetButton.onClick.AddListener(() => ServiceLocator.Get<GameManager>().BuyMons());
        spawnFoodButton.onClick.AddListener(StartFoodPlacement);
        gachaButton.onClick.AddListener(() => ServiceLocator.Get<GachaManager>().RollGacha());
        
        // Subscribe to coin and poop change events
        var gameManager = ServiceLocator.Get<GameManager>();
        gameManager.OnCoinChanged += UpdateCoinCounterValue;
        gameManager.OnPoopChanged += UpdatePoopCounterValue;
        gameManager.OnCoinChanged?.Invoke(gameManager.coinCollected);
        gameManager.OnPoopChanged?.Invoke(gameManager.poopCollected);
    }

    public void ShowMenu()
    {
        if (_isAnimating) return;
        
        _onMenuOpened = !_onMenuOpened;
        
        if (_onMenuOpened)
        {
            StartCoroutine(SlideInMenu());
            // TEMPORARY FEATURE: Scale up and slide up button
            StartCoroutine(AnimateButtonUp());
        }
        else
        {
            StartCoroutine(SlideOutMenu());
            // TEMPORARY FEATURE: Scale down and slide down button
            StartCoroutine(AnimateButtonDown());
        }
    }
    
    // TEMPORARY FEATURE: Button scale up and slide up animation
    private IEnumerator AnimateButtonUp()
    {
        RectTransform buttonRect = UIMenuButton.GetComponent<RectTransform>();
        Vector3 targetScale = _buttonInitialScale * 2f;
        Vector3 targetPosition = _buttonInitialPosition;
        targetPosition.y += 50f; // Slide up by 50 units
        
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = 1f - (1f - t) * (1f - t); // Ease out curve
            
            UIMenuButton.transform.localScale = Vector3.Lerp(_buttonInitialScale, targetScale, t);
            buttonRect.anchoredPosition = Vector3.Lerp(_buttonInitialPosition, targetPosition, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        UIMenuButton.transform.localScale = targetScale;
        buttonRect.anchoredPosition = targetPosition;
    }
    
    // TEMPORARY FEATURE: Button scale down and slide down animation
    private IEnumerator AnimateButtonDown()
    {
        RectTransform buttonRect = UIMenuButton.GetComponent<RectTransform>();
        Vector3 currentScale = UIMenuButton.transform.localScale;
        Vector3 currentPosition = buttonRect.anchoredPosition;
        
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t; // Ease in curve
            
            UIMenuButton.transform.localScale = Vector3.Lerp(currentScale, _buttonInitialScale, t);
            buttonRect.anchoredPosition = Vector3.Lerp(currentPosition, _buttonInitialPosition, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        UIMenuButton.transform.localScale = _buttonInitialScale;
        buttonRect.anchoredPosition = _buttonInitialPosition;
    }

    // Add these new methods to handle the event callbacks
    private void UpdateCoinCounterValue(int newCoinAmount)
    {
        coinCounterText.text = $"Coin : {newCoinAmount}";
    }

    private void UpdatePoopCounterValue(int newPoopAmount)
    {
        poopCounterText.text = $"Poop : {newPoopAmount}";
    }

    private IEnumerator SlideInMenu()
    {
        _isAnimating = true;
        UIMenuPanel.SetActive(true);

        RectTransform rectTransform = UIMenuPanel.GetComponent<RectTransform>();
        Vector3 startPos = _menuInitialPosition;
        startPos.y -= rectTransform.rect.height; // Move below screen

        rectTransform.anchoredPosition = startPos;
        UIMenuCanvasGroup.alpha = 0f;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = 1f - (1f - t) * (1f - t); // Ease out curve

            rectTransform.anchoredPosition = Vector3.Lerp(startPos, _menuInitialPosition, t);
            UIMenuCanvasGroup.alpha = t;

            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = _menuInitialPosition;
        UIMenuCanvasGroup.alpha = 1f;
        _isAnimating = false;
    }
    
    private IEnumerator SlideOutMenu()
    {
        _isAnimating = true;
        
        RectTransform rectTransform = UIMenuPanel.GetComponent<RectTransform>();
        Vector3 targetPos = _menuInitialPosition;
        targetPos.y -= rectTransform.rect.height; // Move below screen
        
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t; // Ease in curve
            
            rectTransform.anchoredPosition = Vector3.Lerp(_menuInitialPosition, targetPos, t);
            UIMenuCanvasGroup.alpha = 1f - t;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        UIMenuPanel.SetActive(false);
        _isAnimating = false;
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
        // Unsubscribe from events to prevent memory leaks
        var gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnCoinChanged -= UpdateCoinCounterValue;
            gameManager.OnPoopChanged -= UpdatePoopCounterValue;
        }
        
        ServiceLocator.Unregister<UIManager>();
    }
}