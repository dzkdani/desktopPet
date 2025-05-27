using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System;
using Spine;
using Spine.Unity;

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
    public bool isFinalForm;
    public int evolutionLevel;

    [Header("Monster Visual")]
    public GameObject hungerInfo;
    [SerializeField] private TextMeshProUGUI hungerText;
    public Image monsterImage;
    public Color normalColor = Color.white;
    public Color hungryColor = Color.red;
    public float colorChangeThreshold = 50f;
    public string monsterID;
    public SkeletonGraphic monsterSpine;

    private float _foodDetectionRangeSqr;
    private float _eatDistanceSqr;
    private Image _monsterImage;
    private SkeletonGraphic _monsterSpineGraphic;
    private TextMeshProUGUI _hungerText;
    private CanvasGroup _hungerInfoCg;
    private RectTransform rectTransform;
    private Vector2 targetPosition;
    private FoodController nearestFood;
    private float groundPos; // Stores the bottom position

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
    private bool isLoaded = false;

    private void Start()
    {
        ServiceLocator.Get<GameManager>().RegisterToActiveMons(this);
        rectTransform = GetComponent<RectTransform>();
        groundPos = ServiceLocator.Get<GameManager>().gameArea.rect.yMin + rectTransform.rect.height / 2;
        if (ServiceLocator.Get<GameManager>().isActiveAndEnabled)
        {
            isLoaded = true;
        }

        LoadMonData();
        SetRandomTarget();

        monsterImage.color = normalColor;
        _monsterImage = monsterImage;

        // Assign Spine reference
        _monsterSpineGraphic = monsterSpine;

        // Initialize Spine animation to idle
        if (_monsterSpineGraphic != null)
        {
            _monsterSpineGraphic.AnimationState.SetAnimation(0, "idle", true);
        }

        //hunger info setup
        _hungerText = hungerText;
        _hungerInfoCg = hungerInfo.GetComponent<CanvasGroup>();
        _hungerInfoCg.alpha = 0;

        //food detection setup
        _foodDetectionRangeSqr = foodDetectionRange * foodDetectionRange;
        _eatDistanceSqr = eatDistance * eatDistance;
    }

    private void OnEnable()
    {
        if (isLoaded)
        {
        }
        _foodRoutine = StartCoroutine(FoodScanLoop());
        _hungerRoutine = StartCoroutine(HungerRoutine());
        _poopRoutine = StartCoroutine(PoopRoutine());
        _goldCoinRoutine = StartCoroutine(CoinCoroutine((float)TimeSpan.FromMinutes(30).TotalSeconds, CoinType.Gold));
        _silverCoinRoutine = StartCoroutine(CoinCoroutine((float)TimeSpan.FromMinutes(1).TotalSeconds, CoinType.Silver));

        // OnHungerChanged += UpdateColor;
        OnHoverChanged += ToggleHungerUI;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        // OnHungerChanged -= UpdateColor;
        OnHoverChanged -= ToggleHungerUI;
    }

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
            // change sprite color via cached _monImage
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
        var wait = new WaitForSeconds(2f); // every 2000 ms
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

    public void SaveMonData()
    {
        var data = new MonsterSaveData
        {
            monsterId = monsterID,
            lastHunger = currentHunger,
            isEvolved = isEvolved, // Track evolution state
            isFinalForm = isFinalForm, // Track final form state
            evolutionLevel = evolutionLevel // Track evolution level

        };
        SaveSystem.SaveMon(data);
    }

    public void LoadMonData()
    {
        if (monsterData == null)
        {
            Debug.LogError($"No MonsterDataSO found for monID: {monsterID}");
            return;
        }

        // Step 2: Load runtime data (hunger, evolution state)
        if (SaveSystem.LoadMon(monsterID, out MonsterSaveData savedData))
        {
            currentHunger = savedData.lastHunger;
            monsterID = savedData.monsterId;
            monsterData.isEvolved = savedData.isEvolved;
            monsterData.isFinalEvol = savedData.isFinalForm;
            monsterData.evolutionLevel = savedData.evolutionLevel;
        }
        else
        {
            currentHunger = 100f;
            monsterData.isEvolved = false;
            monsterData.isFinalEvol = false;
            monsterData.evolutionLevel = 0;
        }

        // Step 3: Apply monsterData values to runtime
        moveSpeed = monsterData.moveSpd;
        hungerDepletionRate = monsterData.hungerDepleteRate;
        poopInterval = monsterData.poopRate;

        // Set correct image
        if (monsterData.monsImgs != null && monsterData.monsImgs.Length > 0)
        {
            int imgIndex = monsterData.isEvolved ? 1 : 0;
            imgIndex = Mathf.Clamp(imgIndex, 0, monsterData.monsImgs.Length - 1);
            monsterImage.sprite = monsterData.monsImgs[imgIndex];
        }
    }

    private void Update()
    {
        HandleMovement();
    }

    private void FindNearestFood()
    {
        if (!isLoaded)
            return;

        nearestFood = null;
        float closestSqr = float.MaxValue;
        Vector2 pos = rectTransform.anchoredPosition;

        foreach (FoodController food in ServiceLocator.Get<GameManager>().activeFoods)
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
        Vector2 target = new Vector2(targetPosition.x, groundPos);

        // Move horizontally while maintaining bottom position
        rectTransform.anchoredPosition = Vector2.MoveTowards(
            pos, target, moveSpeed * Time.deltaTime);

        // --- SPINE ANIMATION LOGIC ---
        if (_monsterSpineGraphic != null)
        {
            float distance = Vector2.Distance(pos, target);
            if (distance > 1f)
            {
                // Play walk animation if not already playing
                var current = _monsterSpineGraphic.AnimationState.GetCurrent(0);
                if (current == null || current.Animation.Name != "walking")
                    _monsterSpineGraphic.AnimationState.SetAnimation(0, "walking", true);

                // Flip skeleton based on direction
                float dir = target.x - pos.x;
                if (Mathf.Abs(dir) > 0.1f)
                {
                    Vector3 scale = rectTransform.localScale;
                    scale.x = Mathf.Abs(scale.x) * (dir < 0 ? 1f : -1f);
                    rectTransform.localScale = scale;
                }
            }
            else
            {
                // Play idle animation if not already playing
                var current = _monsterSpineGraphic.AnimationState.GetCurrent(0);
                if (current == null || current.Animation.Name != "idle")
                    _monsterSpineGraphic.AnimationState.SetAnimation(0, "idle", true);
            }
        }

        // Check if reached target
        if (Mathf.Abs(rectTransform.anchoredPosition.x - targetPosition.x) < 10f)
        {
            // Check if reached food
            if (nearestFood != null &&
                Vector2.Distance(rectTransform.anchoredPosition, nearestFood.GetComponent<RectTransform>().anchoredPosition) < eatDistance)
            {
                Feed(nearestFood.nutritionValue);
                ServiceLocator.Get<GameManager>().DespawnFood(nearestFood.gameObject);
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
                ServiceLocator.Get<GameManager>().DespawnFood(nearestFood.gameObject);
                nearestFood = null;
                SetRandomTarget();
            }
        }
    }

    // private void UpdateColor(float hunger)
    // {
    //     _monsterImage.color = (hunger <= colorChangeThreshold)
    //         ? Color.Lerp(normalColor, hungryColor, 1 - (hunger / colorChangeThreshold))
    //         : normalColor;
    // }

    private void ToggleHungerUI(bool show)
    {
        _hungerInfoCg.alpha = show ? 1 : 0;
        if (show)
            _hungerText.text = $"Hunger: {currentHunger:F1}%";
    }

    private void SetRandomTarget()
    {
        targetPosition = new Vector2(
            UnityEngine.Random.Range(ServiceLocator.Get<GameManager>().gameArea.rect.xMin, ServiceLocator.Get<GameManager>().gameArea.rect.xMax), groundPos);
    }

    public void Feed(float amount)
    {
        currentHunger = Mathf.Clamp(currentHunger + amount, 0f, 100f);
    }

    private void Poop()
    {
        ServiceLocator.Get<GameManager>().SpawnPoopAt(rectTransform.anchoredPosition);
    }

    private void DropCoin(CoinType type)
    {
        ServiceLocator.Get<GameManager>().SpawnCoinAt(rectTransform.anchoredPosition, type);
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
    public bool isFinalForm;
    public int evolutionLevel;
}


