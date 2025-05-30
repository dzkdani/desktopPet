using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System;
using Spine.Unity;

[System.Serializable]
public class MonsterSaveData
{
    public string monsterId;
    public float lastHunger;
    public bool isEvolved;
    public bool isFinalForm;
    public int evolutionLevel;
}

public class MonsterController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Monster Data")]
    public float hungerDepletionRate = 0.1f;
    public float poopInterval = 20f;
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
    public Color normalColor = Color.white;
    public Color hungryColor = Color.red;
    public string monsterID;
    public SkeletonGraphic monsterSpine;

    [Header("State Machine")]
    public MonsterStateMachine stateMachine;

    // Cached Components
    private SkeletonGraphic _monsterSpineGraphic;
    private TextMeshProUGUI _hungerText;
    private CanvasGroup _hungerInfoCg;
    private RectTransform rectTransform;
    private GameManager _gameManager;
    private RectTransform _nearestFoodRect;

    // Movement & Food Detection
    private Vector2 targetPosition;
    private float _foodDetectionRangeSqr;
    private float _eatDistanceSqr;
    private float _cachedFoodDistanceSqr = float.MaxValue;
    private bool _isNearFood = false;
    private bool isLoaded = false;
    private bool _shouldDropCoinAfterPoke = false; // Add this flag

    // Poke cooldown
    private float _pokeCooldownTimer = 0f;
    private const float POKE_COOLDOWN_DURATION = 5f; // 5 seconds cooldown for poke action

    // Coroutines
    private Coroutine _hungerRoutine;
    private Coroutine _poopRoutine;
    private Coroutine _foodRoutine;
    private Coroutine _goldCoinRoutine;
    private Coroutine _silverCoinRoutine;

    private readonly WaitForSeconds _foodScanWait = new WaitForSeconds(2f);
    private readonly WaitForSeconds _hungerWait = new WaitForSeconds(1f);

    // Properties
    public FoodController nearestFood { get; private set; }

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

    public void OnPointerEnter(PointerEventData e) => isHovered = true;
    public void OnPointerExit(PointerEventData e) => isHovered = false;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isHovered)
        {
            Poke();
        }
    }

    #region Initialization
    private void Awake()
    {
        // Generate ID if not already set (for new monsters)
        if (string.IsNullOrEmpty(monsterID))
        {
            monsterID = System.Guid.NewGuid().ToString();
        }
    }

    private void Start()
    {
        InitializeComponents();
        InitializeStateMachine();
        InitializeValues();
        SetRandomTarget();
    }

    private void InitializeComponents()
    {
        // Cache component references
        rectTransform = GetComponent<RectTransform>();
        _monsterSpineGraphic = monsterSpine;
        _hungerText = hungerText;
        _hungerInfoCg = hungerInfo.GetComponent<CanvasGroup>();
        
        // Initialize UI
        _hungerInfoCg.alpha = 0;
        
        // Initialize Spine animation
        if (_monsterSpineGraphic != null)
            _monsterSpineGraphic.AnimationState.SetAnimation(0, "idle", true);
    }

    private void InitializeStateMachine()
    {
        stateMachine = GetComponent<MonsterStateMachine>();
        if (stateMachine != null)
            stateMachine.OnStateChanged += OnStateChanged;
    }

    private void InitializeValues()
    {
        // Register with GameManager
        _gameManager = ServiceLocator.Get<GameManager>();
        _gameManager?.RegisterToActiveMons(this);
        
        if (_gameManager != null && _gameManager.isActiveAndEnabled) 
            isLoaded = true;

        // Setup food detection ranges
        _foodDetectionRangeSqr = foodDetectionRange * foodDetectionRange;
        _eatDistanceSqr = eatDistance * eatDistance;
    }
    #endregion

    #region Lifecycle
    private void OnEnable()
    {
        StartCoroutines();
        OnHoverChanged += ToggleHungerUI;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        OnHoverChanged -= ToggleHungerUI;
    }

    private void Update()
    {
        if (!isLoaded) return;
        
        // Update poke cooldown timer
        if (_pokeCooldownTimer > 0f)
        {
            _pokeCooldownTimer -= Time.deltaTime;
        }
        
        HandleMovement();
    }

    private void StartCoroutines()
    {
        _foodRoutine = StartCoroutine(FoodScanLoop());
        _hungerRoutine = StartCoroutine(HungerRoutine());
        _poopRoutine = StartCoroutine(PoopRoutine());
        _goldCoinRoutine = StartCoroutine(CoinCoroutine((float)TimeSpan.FromMinutes((double)CoinType.Gold).TotalSeconds, CoinType.Gold));
        _silverCoinRoutine = StartCoroutine(CoinCoroutine((float)TimeSpan.FromMinutes((double)CoinType.Silver).TotalSeconds, CoinType.Silver));
    }
    #endregion

    #region Movement & Animation
    private void HandleMovement()
    {
        Vector2 pos = rectTransform.anchoredPosition;
        Vector2 target = targetPosition;
        
        // Get movement speed based on current state
        float currentSpeed = GetCurrentMoveSpeed();
        
        // Move toward target
        rectTransform.anchoredPosition = Vector2.MoveTowards(pos, target, currentSpeed * Time.deltaTime);
        
        // Handle state-specific behavior
        HandleStateSpecificBehavior(pos, target);
        
        // Handle food logic (only if in walking/running state)
        if (CanMoveToFood())
        {
            HandleFoodLogic(pos);
        }
        
        // Check if reached target
        if (Vector2.Distance(pos, targetPosition) < 10f)
        {
            SetRandomTarget();
        }
    }

    private bool CanMoveToFood()
    {
        return stateMachine.CurrentState == MonsterState.Walking || 
               stateMachine.CurrentState == MonsterState.Running;
    }

    private float GetCurrentMoveSpeed()
    {
        if (stateMachine == null) return moveSpeed;
        
        return stateMachine.CurrentState switch
        {
            MonsterState.Walking => stateMachine.behaviorConfig.walkSpeed,
            MonsterState.Running => stateMachine.behaviorConfig.runSpeed,
            MonsterState.Jumping => 0f, // No movement during jumping
            _ => 0f // Idle, Eating, Itching don't move
        };
    }

    private void HandleStateSpecificBehavior(Vector2 pos, Vector2 target)
    {
        if (_monsterSpineGraphic == null) return;
        
        var state = stateMachine?.CurrentState ?? MonsterState.Idle;
        
        // Update animation
        UpdateAnimation(state);
        
        // Handle state-specific logic
        switch (state)
        {
            case MonsterState.Jumping:
            case MonsterState.Walking:
            case MonsterState.Running:
            case MonsterState.Itching:
                HandleDirectionalFlipping(pos, target);
                break;
        }
    }

    private void UpdateAnimation(MonsterState state)
    {
        string animationName = GetAnimationForState(state);
        var current = _monsterSpineGraphic.AnimationState.GetCurrent(0);
        
        if (current == null || current.Animation.Name != animationName)
        {
            _monsterSpineGraphic.AnimationState.SetAnimation(0, animationName, true);
        }
    }

    private string GetAnimationForState(MonsterState state)
    {
        return state switch
        {
            MonsterState.Idle => "idle",
            MonsterState.Walking => "walking",
            MonsterState.Running => "running",
            MonsterState.Jumping => "jumping",
            MonsterState.Itching => "itching",
            MonsterState.Eating => "eating",
            _ => "idle"
        };
    }

    private void HandleDirectionalFlipping(Vector2 pos, Vector2 target)
    {
        if (_monsterSpineGraphic == null) return;
        
        float direction = target.x - pos.x;
        
        if (Mathf.Abs(direction) > 0.1f)
        {
            Transform parentTransform = _monsterSpineGraphic.transform.parent;
            Transform targetTransform = parentTransform ?? _monsterSpineGraphic.transform;
            
            Vector3 scale = targetTransform.localScale;
            scale.x = direction > 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            targetTransform.localScale = scale;
        }
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
    }
    #endregion

    #region Food Logic
    private void HandleFoodLogic(Vector2 pos)
    {
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
                _nearestFoodRect = foodRt;
            }
        }
        
        _cachedFoodDistanceSqr = nearestFood != null ? closestSqr : float.MaxValue;
        _isNearFood = _cachedFoodDistanceSqr < _eatDistanceSqr;
    }

    public void Feed(float amount)
    {
        currentHunger = Mathf.Clamp(currentHunger + amount, 0f, 100f);
    }
    #endregion

    #region Save/Load System
    public void SaveMonData()
    {
        var data = new MonsterSaveData
        {
            monsterId = monsterID,
            lastHunger = currentHunger,
            isEvolved = isEvolved,
            isFinalForm = isFinalForm,
            evolutionLevel = evolutionLevel
        };
        SaveSystem.SaveMon(data);
    }

    public void LoadMonData()
    {
        if (monsterData == null)
        {
            InitializeAsNewMonster();
            return;
        }

        if (SaveSystem.LoadMon(monsterID, out MonsterSaveData savedData))
        {
            LoadFromSaveData(savedData);
        }
        else
        {
            InitializeAsNewMonster();
        }

        ApplyMonsterDataStats();
    }

    private void LoadFromSaveData(MonsterSaveData savedData)
    {
        currentHunger = savedData.lastHunger;
        monsterID = savedData.monsterId;
        monsterData.isEvolved = savedData.isEvolved;
        monsterData.isFinalEvol = savedData.isFinalForm;
        monsterData.evolutionLevel = savedData.evolutionLevel;
    }

    private void InitializeAsNewMonster()
    {
        currentHunger = 100f;
        if (monsterData != null)
        {
            monsterData.isEvolved = false;
            monsterData.isFinalEvol = false;
            monsterData.evolutionLevel = 0;
        }
    }

    private void ApplyMonsterDataStats()
    {
        if (monsterData != null)
        {
            moveSpeed = monsterData.moveSpd;
            hungerDepletionRate = monsterData.hungerDepleteRate;
            poopInterval = monsterData.poopRate;
        }
    }
    #endregion

    #region Coroutines
    private IEnumerator HungerRoutine()
    {
        while (true)
        {
            currentHunger = Mathf.Clamp(currentHunger - hungerDepletionRate, 0f, 100f);
            yield return _hungerWait;
            
            UpdateHungerUI();
        }
    }

    private IEnumerator PoopRoutine()
    {
        yield return new WaitForSeconds(20f); // Initial delay
        
        while (true)
        {
            Poop();
            yield return new WaitForSeconds(20f);
        }
    }

    private IEnumerator FoodScanLoop()
    {
        while (true)
        {
            FindNearestFood();
            yield return _foodScanWait;
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
    #endregion

    #region Actions
    private void Poop() => ServiceLocator.Get<GameManager>().SpawnPoopAt(rectTransform.anchoredPosition);

    private void DropCoin(CoinType type) => ServiceLocator.Get<GameManager>().SpawnCoinAt(rectTransform.anchoredPosition, type);

    public void Poke()
    {
        // Check if poke is on cooldown
        if (_pokeCooldownTimer > 0f)
        {
            return; // Ignore poke if on cooldown
        }

        // Start poke cooldown
        _pokeCooldownTimer = POKE_COOLDOWN_DURATION;

        // Set flag to drop coin after animation finishes
        _shouldDropCoinAfterPoke = true;
        
        // Choose random poke animation (jumping or itching)
        MonsterState pokeState = UnityEngine.Random.Range(0, 2) == 0 ? MonsterState.Jumping : MonsterState.Itching;
        
        // Force the state machine to change to the poke state
        if (stateMachine != null)
        {
            stateMachine.ForceState(pokeState);
        }
    }

    private void OnStateChanged(MonsterState newState)
    {
        // Check if we just finished a poke animation and need to drop a coin
        if (_shouldDropCoinAfterPoke && 
            (stateMachine.PreviousState == MonsterState.Jumping || stateMachine.PreviousState == MonsterState.Itching) &&
            newState != MonsterState.Jumping && newState != MonsterState.Itching)
        {
            DropCoin(CoinType.Silver);
            _shouldDropCoinAfterPoke = false;
        }

        switch (newState)
        {
            case MonsterState.Jumping:
                // Don't move during jumping - movement speed is set to 0
                break;
            case MonsterState.Eating:
                // Stop moving while eating
                break;
        }
    }
    #endregion

    #region UI
    private void UpdateHungerUI()
    {
        _hungerInfoCg.alpha = isHovered ? 1 : 0;
        _hungerText.text = $"Hunger: {currentHunger:F1}%";
    }

    private void ToggleHungerUI(bool show)
    {
        _hungerInfoCg.alpha = show ? 1 : 0;
        if (show)
            _hungerText.text = $"Hunger: {currentHunger:F1}%";
    }
    #endregion
}


