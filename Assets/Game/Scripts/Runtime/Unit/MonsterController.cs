using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System;
using Spine;
using Spine.Unity;
using System.Collections.Generic;

[System.Serializable]
public class MonsterSaveData
{
    public string monsterId;
    public float lastHunger;
    public bool isEvolved;
    public bool isFinalForm;
    public int evolutionLevel;
}

public class MonsterController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Monster Data")]
    public float hungerDepletionRate = 0.1f;
    public float poopInterval = 20f; //Minutes
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
    private SkeletonGraphic _monsterSpineGraphic;
    private TextMeshProUGUI _hungerText;
    private CanvasGroup _hungerInfoCg;
    private RectTransform rectTransform;
    private Vector2 targetPosition;
    private FoodController nearestFood;

    private Coroutine _hungerRoutine;
    private Coroutine _poopRoutine;
    private Coroutine _foodRoutine;
    private Coroutine _goldCoinRoutine;
    private Coroutine _silverCoinRoutine;

    // Add these private variables at the top
    private float _cachedFoodDistanceSqr = float.MaxValue;
    private bool _isNearFood = false;

    // Add these cached references
    private GameManager _gameManager;
    private RectTransform _nearestFoodRect;

    // Cache frequently used objects
    private readonly WaitForSeconds _foodScanWait = new WaitForSeconds(2f);
    private readonly WaitForSeconds _hungerWait = new WaitForSeconds(1f);

    // Cache animation names at start
    private readonly HashSet<string> _movementAnimations = new HashSet<string> 
    { 
        "walking", "walking2", "walking3", "running", "running2" 
    };

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
        // Cache GameManager reference
        _gameManager = ServiceLocator.Get<GameManager>();
        _gameManager.RegisterToActiveMons(this);
        
        rectTransform = GetComponent<RectTransform>();
        // Remove groundPos since we now move in 2D
        if (_gameManager.isActiveAndEnabled) isLoaded = true;

        LoadMonData();
        SetRandomTarget();

        // Assign Spine reference
        _monsterSpineGraphic = monsterSpine;

        // Initialize Spine animation to idle
        if (_monsterSpineGraphic != null)
            _monsterSpineGraphic.AnimationState.SetAnimation(0, "idle", true);

        //hunger info setup
        _hungerText = hungerText;
        _hungerInfoCg = hungerInfo.GetComponent<CanvasGroup>();
        _hungerInfoCg.alpha = 0;

        //food detection setup
        _foodDetectionRangeSqr = foodDetectionRange * foodDetectionRange;
        _eatDistanceSqr = eatDistance * eatDistance;

        // Cache available movement animations
        CacheAvailableAnimations();
    }

    private void OnEnable()
    {
        _foodRoutine = StartCoroutine(FoodScanLoop());
        _hungerRoutine = StartCoroutine(HungerRoutine());
        _poopRoutine = StartCoroutine(PoopRoutine((float)TimeSpan.FromMinutes(poopInterval).TotalSeconds));
        _goldCoinRoutine = StartCoroutine(CoinCoroutine((float)TimeSpan.FromMinutes((double)CoinType.Gold).TotalSeconds, CoinType.Gold));
        _silverCoinRoutine = StartCoroutine(CoinCoroutine((float)TimeSpan.FromMinutes((double)CoinType.Silver).TotalSeconds, CoinType.Silver));

        OnHoverChanged += ToggleHungerUI;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        OnHoverChanged -= ToggleHungerUI;
    }

    private IEnumerator HungerRoutine()
    {
        while (true)
        {
            currentHunger = Mathf.Clamp(currentHunger - hungerDepletionRate, 0f, 100f);
            yield return _hungerWait; // Use cached WaitForSeconds
            
            _hungerInfoCg.alpha = isHovered ? 1 : 0;
            _hungerText.text = $"Hunger: {currentHunger:F1}%";
        }
    }

    private IEnumerator PoopRoutine(float _delay)
    {
        float _tempDelay = 20f;
        // wait initial interval, then loop
        yield return new WaitForSeconds(_tempDelay);
        while (true)
        {
            Poop();
            yield return new WaitForSeconds(_tempDelay);
        }
    }

    private IEnumerator FoodScanLoop()
    {
        while (true)
        {
            FindNearestFood();
            yield return _foodScanWait; // Use cached WaitForSeconds
        }
    }

    private IEnumerator CoinCoroutine(float _delay, CoinType type)
    {
        yield return new WaitForSeconds(_delay);
        while (true)
        {
            DropCoin(type);
            yield return new WaitForSeconds(_delay);
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
        if (!isLoaded) return;

        nearestFood = null;
        _nearestFoodRect = null;
        float closestSqr = float.MaxValue;
        Vector2 pos = rectTransform.anchoredPosition;

        foreach (FoodController food in _gameManager.activeFoods)
        {
            if (food == null) continue;
            RectTransform foodRt = food.GetComponent<RectTransform>();
            Vector2 foodPos = foodRt.anchoredPosition;

            float sqrDist = (foodPos - pos).sqrMagnitude;
            if (sqrDist < _foodDetectionRangeSqr && sqrDist < closestSqr)
            {
                closestSqr = sqrDist;
                nearestFood = food;
                _nearestFoodRect = foodRt; // Cache the RectTransform
            }
        }
        
        // Update cached distance
        _cachedFoodDistanceSqr = nearestFood != null ? closestSqr : float.MaxValue;
        _isNearFood = _cachedFoodDistanceSqr < _eatDistanceSqr;
    }

    private float _lastAnimationChangeTime = 0f;
    private float _animationChangeInterval = 2f; // Change animation every 2 seconds
    private Vector2 _lastDirection = Vector2.zero;
    private string _currentMovementAnim = "";

    private void HandleMovement()
    {
        Vector2 pos = rectTransform.anchoredPosition;
        Vector2 target = targetPosition;

        // Move in 2D space within game area bounds
        rectTransform.anchoredPosition = Vector2.MoveTowards(
            pos, target, moveSpeed * Time.deltaTime);

        // --- SPINE ANIMATION LOGIC ---
        if (_monsterSpineGraphic != null)
        {
            float distance = Vector2.Distance(pos, target);
            if (distance > 1f)
            {
                // Calculate current direction
                Vector2 currentDirection = (target - pos).normalized;
                
                // Check if we should change animation
                bool shouldChangeAnim = false;
                
                // Change animation if:
                // 1. Not currently playing a movement animation
                // 2. Direction changed significantly
                // 3. Time interval passed
                var current = _monsterSpineGraphic.AnimationState.GetCurrent(0);
                
                if (current == null || !IsMovementAnimation(current.Animation.Name))
                {
                    shouldChangeAnim = true;
                }
                else if (Vector2.Dot(_lastDirection, currentDirection) < 0.8f) // Direction changed
                {
                    shouldChangeAnim = true;
                }
                else if (Time.time - _lastAnimationChangeTime > _animationChangeInterval)
                {
                    shouldChangeAnim = true;
                }
                
                if (shouldChangeAnim)
                {
                    string newAnim = GetRandomMovementAnimation();
                    if (newAnim != _currentMovementAnim) // Only change if different
                    {
                        _monsterSpineGraphic.AnimationState.SetAnimation(0, newAnim, true);
                        _currentMovementAnim = newAnim;
                        _lastAnimationChangeTime = Time.time;
                        Debug.Log($"Changed to animation: {newAnim}");
                    }
                }
                
                _lastDirection = currentDirection;

                // Flip skeleton based on horizontal direction
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
                {
                    _monsterSpineGraphic.AnimationState.SetAnimation(0, "idle", true);
                    _currentMovementAnim = "idle";
                }
            }
        }

        // Simplified food handling using cached values
        if (nearestFood != null)
        {
            if (_isNearFood)
            {
                Feed(nearestFood.nutritionValue);
                ServiceLocator.Get<GameManager>().DespawnPools(nearestFood.gameObject);
                nearestFood = null;
                SetRandomTarget();
            }
            else
            {
                // Move toward food
                targetPosition = nearestFood.GetComponent<RectTransform>().anchoredPosition;
            }
        }
        else if (Vector2.Distance(pos, targetPosition) < 10f)
        {
            SetRandomTarget();
        }
    }

    private bool IsMovementAnimation(string animName)
    {
        return _movementAnimations.Contains(animName);
    }

    // Cache available animations at start
    private List<string> _availableMovementAnims;

    private void CacheAvailableAnimations()
    {
        _availableMovementAnims = new List<string>();
        
        if (_monsterSpineGraphic?.Skeleton?.Data?.Animations != null)
        {
            foreach (string animName in _movementAnimations)
            {
                if (HasAnimation(animName))
                    _availableMovementAnims.Add(animName);
            }
        }
        
        // Ensure we have at least one fallback
        if (_availableMovementAnims.Count == 0)
        {
            if (HasAnimation("walking")) _availableMovementAnims.Add("walking");
            else if (HasAnimation("running")) _availableMovementAnims.Add("running");
            else _availableMovementAnims.Add("idle");
        }
    }

    private string GetRandomMovementAnimation()
    {
        if (_availableMovementAnims.Count == 0)
            return "idle";
            
        return _availableMovementAnims[UnityEngine.Random.Range(0, _availableMovementAnims.Count)];
    }

    private bool HasAnimation(string animName)
    {
        if (_monsterSpineGraphic?.Skeleton?.Data?.Animations == null)
            return false;

        foreach (var anim in _monsterSpineGraphic.Skeleton.Data.Animations)
        {
            if (anim.Name == animName)
                return true;
        }
        return false;
    }

    private void ToggleHungerUI(bool show)
    {
        _hungerInfoCg.alpha = show ? 1 : 0;
        if (show)
            _hungerText.text = $"Hunger: {currentHunger:F1}%";
    }

    private void SetRandomTarget()
    {
        var gameAreaRect = _gameManager.gameArea;
        Vector2 size = gameAreaRect.sizeDelta;
        
        // Calculate bounds from center (0,0) of gameArea
        float halfWidth = rectTransform.rect.width / 2;
        float halfHeight = rectTransform.rect.height / 2;
        
        float minX = -size.x / 2 + halfWidth;
        float maxX = size.x / 2 - halfWidth;
        float minY = -size.y / 2 + halfHeight;
        float maxY = size.y / 2 - halfHeight;

        targetPosition = new Vector2(
            UnityEngine.Random.Range(minX, maxX),
            UnityEngine.Random.Range(minY, maxY)
        );
        
        // Force animation change on new target
        _lastAnimationChangeTime = 0f; // This will trigger animation change immediately
        Debug.Log($"New target set: {targetPosition}");
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
}


