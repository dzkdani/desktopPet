using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System;
using Spine.Unity;

#region Data Classes
[System.Serializable]
public class MonsterSaveData
{
    public string monsterId;
    public float lastHunger;
    public float lastHappiness;
    public bool isEvolved;
    public bool isFinalForm;
    public int evolutionLevel;
}

[System.Serializable]
public class MonsterStats
{
    [Header("Core Stats")]
    public float hungerDepletionRate = 0.1f;
    public float happinessDepletionRate = 0.05f;
    public float poopInterval = 20f;
    public float hungerThresholdToEat = 30f;
    public float happinessThresholdForBehavior = 50f;
    public float moveSpeed = 100f;
    public float foodDetectionRange = 200f;
    public float eatDistance = 30f;
    
    [Header("Happiness System")]
    public float pokeHappinessIncrease = 2f;
    public float areaHappinessRate = 0.2f;
    
    [Header("Interaction")]
    public float pokeCooldownDuration = 5f;
}

[System.Serializable]
public class MonsterUI
{
    [Header("UI Elements")]
    public GameObject hungerInfo;
    public GameObject happinessInfo;
    [SerializeField] private TextMeshProUGUI hungerText;
    [SerializeField] private TextMeshProUGUI happinessText;
    
    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hungryColor = Color.red;
    public Color sadColor = Color.blue;
    
    // Cached components
    private CanvasGroup _hungerInfoCg;
    private CanvasGroup _happinessInfoCg;
    
    public void Initialize()
    {
        _hungerInfoCg = hungerInfo.GetComponent<CanvasGroup>();
        _happinessInfoCg = happinessInfo.GetComponent<CanvasGroup>();
        
        _hungerInfoCg.alpha = 0;
        _happinessInfoCg.alpha = 0;
    }
    
    public void UpdateHungerDisplay(float hunger, bool show)
    {
        _hungerInfoCg.alpha = show ? 1 : 0;
        if (show)
            hungerText.text = $"Hunger: {hunger:F1}%";
    }
    
    public void UpdateHappinessDisplay(float happiness, bool show)
    {
        _happinessInfoCg.alpha = show ? 1 : 0;
        if (show)
        {
            happinessText.text = $"Happiness: {happiness:F1}%";
            happinessText.color = happiness < 30f ? sadColor : normalColor;
        }
    }
}

[System.Serializable]
public class CoroutineConfig
{
    public float foodScanInterval = 2f;
    public float hungerUpdateInterval = 1f;
    public float happinessUpdateInterval = 1f;
    public float poopInterval = 20f;
    public float goldCoinInterval;
    public float silverCoinInterval;
}
#endregion

#region Helper Classes
public class CoroutineManager
{
    private MonoBehaviour _owner;
    private Coroutine[] _coroutines;
    
    public CoroutineManager(MonoBehaviour owner)
    {
        _owner = owner;
        _coroutines = new Coroutine[6];
    }
    
    public void StartAllCoroutines(MonsterController controller, CoroutineConfig config)
    {
        _coroutines[0] = _owner.StartCoroutine(controller.FoodScanLoop(config.foodScanInterval));
        _coroutines[1] = _owner.StartCoroutine(controller.HungerRoutine(config.hungerUpdateInterval));
        _coroutines[2] = _owner.StartCoroutine(controller.HappinessRoutine(config.happinessUpdateInterval));
        _coroutines[3] = _owner.StartCoroutine(controller.PoopRoutine(config.poopInterval));
        _coroutines[4] = _owner.StartCoroutine(controller.CoinCoroutine(config.goldCoinInterval, CoinType.Gold));
        _coroutines[5] = _owner.StartCoroutine(controller.CoinCoroutine(config.silverCoinInterval, CoinType.Silver));
    }
    
    public void StopAllCoroutines()
    {
        foreach (var coroutine in _coroutines)
        {
            if (coroutine != null)
                _owner.StopCoroutine(coroutine);
        }
    }
}

public class MovementHandler
{
    private RectTransform _transform;
    private MonsterStateMachine _stateMachine;
    private GameManager _gameManager;
    private SkeletonGraphic _spineGraphic;
    
    public MovementHandler(RectTransform transform, MonsterStateMachine stateMachine, GameManager gameManager, SkeletonGraphic spineGraphic)
    {
        _transform = transform;
        _stateMachine = stateMachine;
        _gameManager = gameManager;
        _spineGraphic = spineGraphic;
    }
    
    public void Update(ref Vector2 targetPosition, MonsterStats stats)
    {
        Vector2 pos = _transform.anchoredPosition;
        float currentSpeed = GetCurrentMoveSpeed(stats.moveSpeed);
        
        _transform.anchoredPosition = Vector2.MoveTowards(pos, targetPosition, currentSpeed * Time.deltaTime);
        HandleStateSpecificBehavior(pos, targetPosition);
    }
    
    private float GetCurrentMoveSpeed(float baseSpeed)
    {
        if (_stateMachine == null) return baseSpeed;
        
        return _stateMachine.CurrentState switch
        {
            MonsterState.Walking => _stateMachine.behaviorConfig.walkSpeed,
            MonsterState.Running => _stateMachine.behaviorConfig.runSpeed,
            MonsterState.Jumping => 0f,
            _ => 0f
        };
    }
    
    private void HandleStateSpecificBehavior(Vector2 pos, Vector2 target)
    {
        if (_spineGraphic == null || _stateMachine == null) return;
        
        var state = _stateMachine.CurrentState;
        UpdateAnimation(state);
        HandleDirectionalFlipping(pos, target);
    }
    
    private void UpdateAnimation(MonsterState state)
    {
        string animationName = GetAnimationForState(state);
        var current = _spineGraphic.AnimationState.GetCurrent(0);
        
        if (current == null || current.Animation.Name != animationName)
        {
            _spineGraphic.AnimationState.SetAnimation(0, animationName, true);
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
        float direction = target.x - pos.x;
        
        if (Mathf.Abs(direction) > 0.1f)
        {
            Transform parentTransform = _spineGraphic.transform.parent;
            Transform targetTransform = parentTransform ?? _spineGraphic.transform;
            
            Vector3 scale = targetTransform.localScale;
            scale.x = direction > 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            targetTransform.localScale = scale;
        }
    }
}
#endregion

public class MonsterController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    #region Serialized Fields
    [Header("Monster Configuration")]
    public MonsterStats stats = new MonsterStats();
    public MonsterUI ui = new MonsterUI();
    public MonsterDataSO monsterData;
    public string monsterID;
    public SkeletonGraphic monsterSpine;

    [Header("Evolution")]
    public bool isEvolved;
    public bool isFinalForm;
    public int evolutionLevel;
    #endregion

    #region Cached Components
    private SkeletonGraphic _monsterSpineGraphic;
    private RectTransform _rectTransform;
    private GameManager _gameManager;
    private MonsterStateMachine _stateMachine;
    private MovementHandler _movementHandler;
    private CoroutineManager _coroutineManager;
    #endregion

    #region Movement & Food Detection
    private Vector2 _targetPosition;
    private float _foodDetectionRangeSqr;
    private float _eatDistanceSqr;
    private float _cachedFoodDistanceSqr = float.MaxValue;
    private bool _isNearFood = false;
    private bool _isLoaded = false;
    private bool _shouldDropCoinAfterPoke = false;
    #endregion

    #region Timers
    private float _pokeCooldownTimer = 0f;
    #endregion

    #region Wait Objects
    private readonly WaitForSeconds _foodScanWait = new WaitForSeconds(2f);
    private readonly WaitForSeconds _hungerWait = new WaitForSeconds(1f);
    private readonly WaitForSeconds _happinessWait = new WaitForSeconds(1f);
    #endregion

    #region Properties
    public FoodController nearestFood { get; private set; }

    private float _currentHunger = 100f;
    private float _currentHappiness = 100f;
    private bool _isHovered;

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

    public float currentHappiness
    {
        get => _currentHappiness;
        private set
        {
            if (Mathf.Approximately(_currentHappiness, value)) return;
            _currentHappiness = value;
            OnHappinessChanged?.Invoke(_currentHappiness);
        }
    }

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
    #endregion

    #region Events
    public event Action<float> OnHungerChanged;
    public event Action<float> OnHappinessChanged;
    public event Action<bool> OnHoverChanged;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeID();
        InitializeComponents();
    }

    private void Start()
    {
        InitializeStateMachine();
        InitializeValues();
        SetRandomTarget();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
        StartCoroutines();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        StopAllManagedCoroutines();
    }

    private void Update()
    {
        if (!_isLoaded) return;

        UpdateTimers();
        HandleMovement();
    }
    #endregion

    #region Initialization
    private void InitializeID()
    {
        if (string.IsNullOrEmpty(monsterID))
            monsterID = System.Guid.NewGuid().ToString();
    }

    private void InitializeComponents()
    {
        _rectTransform = GetComponent<RectTransform>();
        _monsterSpineGraphic = monsterSpine;
        
        ui.Initialize();
        
        // Apply monster visuals if monster data is already set
        if (monsterData != null)
        {
            ApplyMonsterVisuals();
        }
        else if (_monsterSpineGraphic != null)
        {
            _monsterSpineGraphic.AnimationState.SetAnimation(0, "idle", true);
        }
    }

    private void InitializeStateMachine()
    {
        _stateMachine = GetComponent<MonsterStateMachine>();
        if (_stateMachine != null)
            _stateMachine.OnStateChanged += OnStateChanged;
    }

    private void InitializeValues()
    {
        _gameManager = ServiceLocator.Get<GameManager>();
        _gameManager?.RegisterToActiveMons(this);

        if (_gameManager != null && _gameManager.isActiveAndEnabled)
            _isLoaded = true;

        _foodDetectionRangeSqr = stats.foodDetectionRange * stats.foodDetectionRange;
        _eatDistanceSqr = stats.eatDistance * stats.eatDistance;

        _movementHandler = new MovementHandler(_rectTransform, _stateMachine, _gameManager, _monsterSpineGraphic);
    }
    #endregion

    #region Event Management
    private void SubscribeToEvents()
    {
        OnHoverChanged += (show) => ui.UpdateHungerDisplay(currentHunger, show);
        OnHoverChanged += (show) => ui.UpdateHappinessDisplay(currentHappiness, show);
    }

    private void UnsubscribeFromEvents()
    {
        OnHoverChanged -= (show) => ui.UpdateHungerDisplay(currentHunger, show);
        OnHoverChanged -= (show) => ui.UpdateHappinessDisplay(currentHappiness, show);
    }
    #endregion

    #region Input Handlers
    public void OnPointerEnter(PointerEventData e) => isHovered = true;
    public void OnPointerExit(PointerEventData e) => isHovered = false;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isHovered) Poke();
    }
    #endregion

    #region Update Methods
    private void UpdateTimers()
    {
        if (_pokeCooldownTimer > 0f)
            _pokeCooldownTimer -= Time.deltaTime;
    }

    private void HandleMovement()
    {
        _movementHandler.Update(ref _targetPosition, stats);

        if (CanMoveToFood())
            HandleFoodLogic();

        if (Vector2.Distance(_rectTransform.anchoredPosition, _targetPosition) < 10f)
            SetRandomTarget();
    }

    private void HandleFoodLogic()
    {
        if (nearestFood == null) return;

        if (_isNearFood)
        {
            Feed(nearestFood.nutritionValue);
            ServiceLocator.Get<GameManager>().DespawnPools(nearestFood.gameObject);
            nearestFood = null;
            SetRandomTarget();
        }
        else
        {
            _targetPosition = nearestFood.GetComponent<RectTransform>().anchoredPosition;
        }
    }

    private bool CanMoveToFood()
    {
        return _stateMachine?.CurrentState == MonsterState.Walking ||
               _stateMachine?.CurrentState == MonsterState.Running;
    }
    #endregion

    #region Core Actions
    public void Feed(float amount)
    {
        currentHunger = Mathf.Clamp(currentHunger + amount, 0f, 100f);
        IncreaseHappiness(amount * 0.3f);
    }

    public void IncreaseHappiness(float amount)
    {
        currentHappiness = Mathf.Clamp(currentHappiness + amount, 0f, 100f);
    }

    public void Poke()
    {
        if (_pokeCooldownTimer > 0f) return;

        _pokeCooldownTimer = stats.pokeCooldownDuration;
        IncreaseHappiness(stats.pokeHappinessIncrease);
        _shouldDropCoinAfterPoke = true;

        MonsterState pokeState = UnityEngine.Random.Range(0, 2) == 0 ?
            MonsterState.Jumping : MonsterState.Itching;

        _stateMachine?.ForceState(pokeState);
    }

    private void Poop() => ServiceLocator.Get<GameManager>().SpawnPoopAt(_rectTransform.anchoredPosition);
    private void DropCoin(CoinType type) => ServiceLocator.Get<GameManager>().SpawnCoinAt(_rectTransform.anchoredPosition, type);
    #endregion

    #region Utility Methods
    private void SetRandomTarget()
    {
        var bounds = CalculateMovementBounds();
        _targetPosition = new Vector2(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            UnityEngine.Random.Range(bounds.min.y, bounds.max.y)
        );
    }

    private (Vector2 min, Vector2 max) CalculateMovementBounds()
    {
        var gameAreaRect = _gameManager.gameArea;
        Vector2 size = gameAreaRect.sizeDelta;

        float halfWidth = _rectTransform.rect.width / 2;
        float halfHeight = _rectTransform.rect.height / 2;

        return (
            new Vector2(-size.x / 2 + halfWidth, -size.y / 2 + halfHeight),
            new Vector2(size.x / 2 - halfWidth, size.y / 2 - halfHeight)
        );
    }

    private void FindNearestFood()
    {
        if (!_isLoaded) return;

        nearestFood = null;
        float closestSqr = float.MaxValue;
        Vector2 pos = _rectTransform.anchoredPosition;

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
            }
        }

        _cachedFoodDistanceSqr = nearestFood != null ? closestSqr : float.MaxValue;
        _isNearFood = _cachedFoodDistanceSqr < _eatDistanceSqr;
    }

    private void UpdateHappinessBasedOnArea()
    {
        var gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager?.gameArea == null) return;

        float gameAreaHeight = gameManager.gameArea.sizeDelta.y;
        float screenHeight = Screen.currentResolution.height;
        float heightRatio = gameAreaHeight / screenHeight;

        if (heightRatio >= 0.5f)
            currentHappiness = Mathf.Clamp(currentHappiness + stats.areaHappinessRate, 0f, 100f);
        else
            currentHappiness = Mathf.Clamp(currentHappiness - stats.areaHappinessRate, 0f, 100f);
    }
    #endregion

    #region State Machine Events
    private void OnStateChanged(MonsterState newState)
    {
        if (_shouldDropCoinAfterPoke &&
            (_stateMachine.PreviousState == MonsterState.Jumping || _stateMachine.PreviousState == MonsterState.Itching) &&
            newState != MonsterState.Jumping && newState != MonsterState.Itching)
        {
            DropCoin(CoinType.Silver);
            _shouldDropCoinAfterPoke = false;
        }
    }
    #endregion

    #region Coroutine Management
    private void StartCoroutines()
    {
        var config = new CoroutineConfig
        {
            goldCoinInterval = (float)TimeSpan.FromMinutes((double)CoinType.Gold).TotalSeconds,
            silverCoinInterval = (float)TimeSpan.FromMinutes((double)CoinType.Silver).TotalSeconds
        };

        _coroutineManager = new CoroutineManager(this);
        _coroutineManager.StartAllCoroutines(this, config);
    }

    private void StopAllManagedCoroutines()
    {
        _coroutineManager?.StopAllCoroutines();
    }
    #endregion

    #region Coroutines
    public IEnumerator HungerRoutine(float interval)
    {
        while (true)
        {
            currentHunger = Mathf.Clamp(currentHunger - stats.hungerDepletionRate, 0f, 100f);
            yield return new WaitForSeconds(interval);
        }
    }

    public IEnumerator HappinessRoutine(float interval)
    {
        while (true)
        {
            UpdateHappinessBasedOnArea();
            yield return new WaitForSeconds(interval);
        }
    }

    public IEnumerator PoopRoutine(float interval)
    {
        yield return new WaitForSeconds(20f);

        while (true)
        {
            Poop();
            yield return new WaitForSeconds(interval);
        }
    }

    public IEnumerator FoodScanLoop(float interval)
    {
        while (true)
        {
            FindNearestFood();
            yield return new WaitForSeconds(interval);
        }
    }

    public IEnumerator CoinCoroutine(float delay, CoinType type)
    {
        yield return new WaitForSeconds(delay);

        while (true)
        {
            DropCoin(type);
            yield return new WaitForSeconds(delay);
        }
    }
    #endregion

    #region Save/Load System
    public void SaveMonData()
    {
        var data = new MonsterSaveData
        {
            monsterId = monsterID,
            lastHunger = currentHunger,
            lastHappiness = currentHappiness,
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
            LoadFromSaveData(savedData);
        else
            InitializeAsNewMonster();

        ApplyMonsterDataStats();
    }

    private void LoadFromSaveData(MonsterSaveData savedData)
    {
        currentHunger = savedData.lastHunger;
        currentHappiness = savedData.lastHappiness;
        monsterID = savedData.monsterId;
        
        // Set evolution state from saved data
        isEvolved = savedData.isEvolved;
        isFinalForm = savedData.isFinalForm;
        evolutionLevel = savedData.evolutionLevel;
        
        if (monsterData != null)
        {
            monsterData.isEvolved = savedData.isEvolved;
            monsterData.isFinalEvol = savedData.isFinalForm;
            monsterData.evolutionLevel = savedData.evolutionLevel;
            
            // Update visuals based on loaded evolution state
            ApplyMonsterVisuals();
        }
    }

    private void InitializeAsNewMonster()
    {
        currentHunger = 100f;
        currentHappiness = 0f;
        if (monsterData != null)
        {
            monsterData.isEvolved = false;
            monsterData.isFinalEvol = false;
            monsterData.evolutionLevel = 0;
        }
    }

    private void ApplyMonsterDataStats()
    {
        if (monsterData == null) return;

        stats.moveSpeed = monsterData.moveSpd;
        stats.hungerDepletionRate = monsterData.hungerDepleteRate;
        stats.poopInterval = monsterData.poopRate;
        stats.pokeHappinessIncrease = monsterData.pokeHappinessValue;
        stats.areaHappinessRate = monsterData.areaHappinessRate;
    }
    #endregion

    #region Monster Type Management
    public void SetMonsterType(MonsterDataSO newMonsterData)
    {
        if (newMonsterData == null) return;
        
        monsterData = newMonsterData;
        ApplyMonsterVisuals();
        ApplyMonsterDataStats();
    }

    private void ApplyMonsterVisuals()
    {
        if (monsterData == null || _monsterSpineGraphic == null) return;
        
        // Apply the appropriate Spine data based on evolution level
        SkeletonDataAsset targetSkeletonData = GetCurrentSkeletonData();
        
        if (targetSkeletonData != null)
        {
            _monsterSpineGraphic.skeletonDataAsset = targetSkeletonData;
            _monsterSpineGraphic.Initialize(true);
            _monsterSpineGraphic.AnimationState.SetAnimation(0, "idle", true);
        }
    }

    private SkeletonDataAsset GetCurrentSkeletonData()
    {
        if (monsterData == null || monsterData.monsterSpine == null || monsterData.monsterSpine.Length == 0) 
            return null;
    
        // Use evolution level as index, with bounds checking
        int index = Mathf.Clamp(evolutionLevel, 0, monsterData.monsterSpine.Length - 1);
        return monsterData.monsterSpine[index];
    }

    public void UpdateMonsterVisuals()
    {
        // Call this when evolution state changes
        ApplyMonsterVisuals();
    }
    #endregion
}


