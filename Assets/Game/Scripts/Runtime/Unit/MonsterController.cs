using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System;
using Unity.Collections;

public class MonsterController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Monster Data")]
    public float hungerDepletionRate = 0.1f;
    public float poopInterval = 1200f;
    public float hungerThresholdToEat = 30f;
    public float moveSpeed = 100f;
    public float foodDetectionRange = 200f;
    public float eatDistance = 30f;
    public MonsterDataSO monsterData;
    public bool isEvolved;

    [Header("Monster Visual")]
    public GameObject hungerInfo;
    [SerializeField] private TextMeshProUGUI hungerText;
    public Image monsterImage;
    public Color normalColor = Color.white;
    public Color hungryColor = Color.red;
    public float colorChangeThreshold = 50f;
    public string monsterID;

    private float _foodDetectionRangeSqr;
    private float _eatDistanceSqr;
    private Image _monsterImage;
    private TextMeshProUGUI _hungerText;
    private CanvasGroup _hungerInfoCg;
    private RectTransform rectTransform;
    private Vector2 targetPosition;
    private FoodController nearestFood;
    private float bottomYPosition; // Stores the bottom position

    private Coroutine _hungerRoutine;
    private Coroutine _poopRoutine;
    private Coroutine _foodRoutine;
    private Coroutine _goldCoinRoutine;
    private Coroutine _silverCoinRoutine;

    public void OnPointerEnter(PointerEventData e) => isHovered = true;
    public void OnPointerExit(PointerEventData e) => isHovered = false;

    public event Action<float> OnHungerChanged;
    public event Action<bool> OnHoverChanged;

    private float _currentHunger = 100f;
    public float currentHunger
    {
        get => _currentHunger;
        private set
        {
            if (Mathf.Approximately(_currentHunger, value)) return;
            _currentHunger = value;
            OnHungerChanged?.Invoke(_currentHunger);
        }
    }

    private bool _isHovered;
    public bool isHovered
    {
        get => _isHovered;
        private set
        {
            if (_isHovered == value) return;
            _isHovered = value;
            OnHoverChanged?.Invoke(_isHovered);
        }
    }

    private void OnEnable()
    {
        // start both loops
        _hungerRoutine = StartCoroutine(HungerRoutine());
        _poopRoutine = StartCoroutine(PoopRoutine());
        _foodRoutine = StartCoroutine(FoodScanLoop());
        _goldCoinRoutine = StartCoroutine(CoinCoroutine((float)TimeSpan.FromMinutes(30).TotalSeconds, CoinType.Gold));
        _silverCoinRoutine = StartCoroutine(CoinCoroutine((float)TimeSpan.FromMinutes(1).TotalSeconds, CoinType.Silver));

        OnHungerChanged += UpdatePetColor;
        OnHoverChanged += ToggleHungerUI;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        OnHungerChanged -= UpdatePetColor;
        OnHoverChanged -= ToggleHungerUI;
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        GameManager.Instance.RegisterPet(this);
        LoadPetData();
        bottomYPosition = GameManager.Instance.gameArea.rect.yMin + rectTransform.rect.height / 2;
        SetRandomTarget();
        monsterImage.color = normalColor;

        _monsterImage = monsterImage;
        _hungerText = hungerText;
        _hungerInfoCg = hungerInfo.GetComponent<CanvasGroup>();
        _hungerInfoCg.alpha = 0;

        _foodDetectionRangeSqr = foodDetectionRange * foodDetectionRange;
        _eatDistanceSqr = eatDistance * eatDistance;
    }
    // void OnApplicationQuit()
    // {
    //     SavePetData();
    // }

    private IEnumerator HungerRoutine()
    {
        var wait = new WaitForSeconds(1f);
        while (true)
        {
            currentHunger = Mathf.Clamp(currentHunger - hungerDepletionRate, 0f, 100f);
            yield return wait;
            // toggle hunger UI via cached CanvasGroup
            _hungerInfoCg.alpha = isHovered ? 1 : 0;
            // update hunger text via _hungerText
            _hungerText.text = $"Hunger: {currentHunger:F1}%";
            // change sprite color via cached _petImage
            _monsterImage.color = currentHunger <= colorChangeThreshold
                ? Color.Lerp(normalColor, hungryColor, 1 - (currentHunger / colorChangeThreshold))
                : normalColor;
        }
    }

    private IEnumerator PoopRoutine()
    {
        // wait initial interval, then loop
        yield return new WaitForSeconds(poopInterval);

        while (true)
        {
            Poop();
            yield return new WaitForSeconds(poopInterval);
        }
    }

    private IEnumerator FoodScanLoop()
    {
        var wait = new WaitForSeconds(0.2f); // every 200 ms
        while (true)
        {
            FindNearestFood();
            yield return wait;
        }
    }

    private IEnumerator CoinCoroutine(float delay, CoinType type)
    {
        yield return new WaitForSeconds(delay);

        while (true)
        {
            DropCoin(type);
            yield return new WaitForSeconds(delay);
        }
    }

    public void SavePetData()
    {
        var data = new MonsterSaveData
        {
            monsterId = monsterID,
            lastHunger = currentHunger,
            isEvolved = isEvolved // Track evolution state
        };
        SaveSystem.SavePet(data);
    }

    public void LoadPetData()
    {
        if (monsterData == null)
        {
            Debug.LogError($"No MonsterDataSO found for petID: {monsterID}");
            return;
        }

        // Step 2: Load runtime data (hunger, evolution state)
        if (SaveSystem.TryLoadPet(monsterID, out MonsterSaveData savedData))
        {
            currentHunger = savedData.lastHunger;
            monsterID = savedData.monsterId;
            monsterData.isEvolved = savedData.isEvolved; // You can also track evolution per pet
        }
        else
        {
            currentHunger = 100f; // Default
            monsterData.isEvolved = false;
        }

        // Step 3: Apply monsterData values to runtime
        moveSpeed = monsterData.moveSpd;
        hungerDepletionRate = monsterData.hungerDepleteRate;
        poopInterval = monsterData.poopRate;

        // Set correct pet image
        if (monsterData.petImgs != null && monsterData.petImgs.Length > 0)
        {
            int imgIndex = monsterData.isEvolved ? 1 : 0;
            imgIndex = Mathf.Clamp(imgIndex, 0, monsterData.petImgs.Length - 1);
            monsterImage.sprite = monsterData.petImgs[imgIndex];
        }
    }

    private void Update()
    {
        HandleMovement();
        float x = rectTransform.anchoredPosition.x;
    }

    private void FindNearestFood()
    {
        nearestFood = null;
        float closestSqr = float.MaxValue;
        Vector2 pos = rectTransform.anchoredPosition;

        foreach (FoodController food in GameManager.Instance.activeFoods)
        {
            if (food == null) continue;
            RectTransform foodRt = food.GetComponent<RectTransform>();
            Vector2 foodPos = foodRt.anchoredPosition;

            float sqrDist = (foodPos - pos).sqrMagnitude;
            // only consider if within detection range (squared)
            if (sqrDist < _foodDetectionRangeSqr && sqrDist < closestSqr)
            {
                closestSqr = sqrDist;
                nearestFood = food;
            }
        }
    }

    private void HandleMovement()
    {
        Vector2 pos = rectTransform.anchoredPosition;

        // Move horizontally while maintaining bottom position
        rectTransform.anchoredPosition = Vector2.MoveTowards(
            new Vector2(rectTransform.anchoredPosition.x, bottomYPosition),
            new Vector2(targetPosition.x, bottomYPosition),
            moveSpeed * Time.deltaTime);

        // Check if reached target
        if (Mathf.Abs(rectTransform.anchoredPosition.x - targetPosition.x) < 10f)
        {
            // Check if reached food
            if (nearestFood != null &&
                Vector2.Distance(rectTransform.anchoredPosition, nearestFood.GetComponent<RectTransform>().anchoredPosition) < eatDistance)
            {
                Feed(nearestFood.nutritionValue);
                Destroy(nearestFood.gameObject);
            }
            SetRandomTarget();
        }

        // Prioritize food if available (only if it's near our horizontal plane)
        if (nearestFood != null)
        {
            Vector2 foodPos = nearestFood.GetComponent<RectTransform>().anchoredPosition;
            if ((foodPos - pos).sqrMagnitude < _eatDistanceSqr)
            {
                Feed(nearestFood.nutritionValue);
                // return food to pool or destroy
                GameManager.Instance.DespawnFood(nearestFood.gameObject);
                nearestFood = null;
                SetRandomTarget();
            }
        }
    }

    private void UpdatePetColor(float hunger)
    {
        _monsterImage.color = (hunger <= colorChangeThreshold)
            ? Color.Lerp(normalColor, hungryColor, 1 - (hunger / colorChangeThreshold))
            : normalColor;
    }

    private void ToggleHungerUI(bool show)
    {
        _hungerInfoCg.alpha = show ? 1 : 0;
        if (show)
            _hungerText.text = $"Hunger: {currentHunger:F1}%";
    }

    private void SetRandomTarget()
    {
        targetPosition = new Vector2(
            UnityEngine.Random.Range(GameManager.Instance.gameArea.rect.xMin, GameManager.Instance.gameArea.rect.xMax), bottomYPosition);
    }

    public void Feed(float amount)
    {
        currentHunger = Mathf.Clamp(currentHunger + amount, 0f, 100f);
    }

    private void Poop()
    {
        Debug.Log("Pet Poop Time");
        GameManager.Instance.SpawnPoopAt(rectTransform.anchoredPosition);
    }

    private void DropCoin(CoinType type)
    {
        GameManager.Instance.SpawnCoinAt(rectTransform.anchoredPosition, type);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        OnHungerChanged -= UpdatePetColor;
        OnHoverChanged -= ToggleHungerUI;
        GameManager.Instance.activeMonsters.Remove(this);
    }
    public void SetTargetPosition(Vector2 targetPos)
    {
        targetPosition = targetPos;
    }
}
[System.Serializable]
public class MonsterSaveData
{
    public string monsterId;
    public float lastHunger;
    public bool isEvolved;
}


