using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System;
using Spine.Unity;

public class MonsterController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Monster Configuration")]
    public MonsterData stats = new MonsterData();
    public MonsterUIHandler ui = new MonsterUIHandler();
    public string monsterID;
    private MonsterDataSO monsterData;

    [Header("Evolution")]
    public bool isFinalForm;
    public int evolutionLevel;
    public MonsterDataSO MonsterData => monsterData;

    public float currentHunger => _currentHunger;
    public float currentHappiness => _currentHappiness;
    public bool isHovered => _isHovered;
    public bool IsLoaded => _isLoaded;
    public FoodController nearestFood => _foodHandler?.NearestFood;    public event Action<float> OnHungerChanged;
    public bool IsSick => _isSick;
    public event Action<bool> OnSickChanged;
    public event Action<float> OnHappinessChanged;
    public event Action<bool> OnHoverChanged;

    private MonsterSaveHandler _saveHandler;
    private MonsterVisualHandler _visualHandler;
    private MonsterFoodHandler _foodHandler;
    private MonsterInteractionHandler _interactionHandler;
    private MonsterMovementBounds _movementBounds;
    private MonsterEvolutionHandler _evolutionHandler;
    private MonsterSeparationBehavior _separationBehavior;

    private SkeletonGraphic _monsterSpineGraphic;
    private RectTransform _rectTransform;
    private GameManager _gameManager;
    private MonsterStateMachine _stateMachine;
    private MonsterMovement _movementHandler;

    private Coroutine _hungerCoroutine;
    private Coroutine _happinessCoroutine;
    private Coroutine _poopCoroutine;
    private Coroutine _goldCoinCoroutine;
    private Coroutine _silverCoinCoroutine;

    private Vector2 _targetPosition;
    private bool _isLoaded = false;
    private float _currentHunger = 100f;
    private float _currentHappiness = 100f;
    private bool _isHovered;
    private Vector2 _lastSortPosition;
    private float _depthSortThreshold = 20f;
    private bool _isSick = false;
    private float _lowHungerTime = 0f;
    private const float SICK_HUNGER_THRESHOLD = 15f;
    private const float SICK_THRESHOLD_TIME = 600f; // 10 minute of low hunger to get sick


    private void Awake()
    {
        InitializeID();
        InitializeComponents();
        InitializeModules();
    }

    private void Start()
    {
        InitializeStateMachine();
        InitializeValues();
        SetRandomTarget();
        
        if (monsterData != null)
        {
            _visualHandler?.ApplyMonsterVisuals();
        }
    }

    private void OnEnable()
    {
        SubscribeToEvents();
        StartCoroutines();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        StopManagedCoroutines();
    }

    private void Update()
    {
        if (!_isLoaded) return;

        _interactionHandler?.UpdateTimers(Time.deltaTime);
        _evolutionHandler?.UpdateEvolutionTracking(Time.deltaTime);
        HandleMovement();
    }

    private void InitializeModules()
    {
        _saveHandler = new MonsterSaveHandler(this);
        _visualHandler = new MonsterVisualHandler(this, _monsterSpineGraphic);
        _interactionHandler = new MonsterInteractionHandler(this, _stateMachine);
        _separationBehavior = new MonsterSeparationBehavior(this, _gameManager, _rectTransform);
    }

    private void InitializeID()
    {
        if (string.IsNullOrEmpty(monsterID))
        {
            monsterID = "temp_" + System.Guid.NewGuid().ToString("N")[..8];
        }
    }

    private void InitializeComponents()
    {
        _rectTransform = GetComponent<RectTransform>();
        _monsterSpineGraphic = GetComponentInChildren<SkeletonGraphic>();
        
        ui.Init();
    }

    private void InitializeStateMachine()
    {
        _stateMachine = GetComponent<MonsterStateMachine>();
        if (_stateMachine != null)
        {
            _interactionHandler = new MonsterInteractionHandler(this, _stateMachine);
        }
    }

    private void InitializeValues()
    {
        _gameManager = ServiceLocator.Get<GameManager>();
        _gameManager?.RegisterActiveMonster(this);

        if (_gameManager != null && _gameManager.isActiveAndEnabled)
            _isLoaded = true;

        _foodHandler = new MonsterFoodHandler(this, _gameManager, _rectTransform);
        _foodHandler.Initialize(stats);
        
        _movementBounds = new MonsterMovementBounds(_rectTransform, _gameManager);
        _movementHandler = new MonsterMovement(_rectTransform, _stateMachine, _gameManager, _monsterSpineGraphic);
        _separationBehavior = new MonsterSeparationBehavior(this, _gameManager, _rectTransform); // Add this
    }

    private void SubscribeToEvents()
    {
        // Update text continuously but don't adjust positions
        OnHungerChanged += (hunger) => ui.UpdateHungerDisplay(hunger, _isHovered);
        OnHappinessChanged += (happiness) => ui.UpdateHappinessDisplay(happiness, _isHovered);
        
        // Add sick status event if you have UI for it
        OnSickChanged += (isSick) => {
            // Handle sick status UI updates here if needed
            Debug.Log($"Monster sick status changed: {isSick}");
        };
        
        // Control visibility on hover
        OnHoverChanged += (hovered) => {
            ui.UpdateHungerDisplay(currentHunger, hovered);
            ui.UpdateHappinessDisplay(currentHappiness, hovered);
        };
    }

    private void UnsubscribeFromEvents()
    {
        OnHungerChanged -= (hunger) => ui.UpdateHungerDisplay(hunger, _isHovered);
        OnHappinessChanged -= (happiness) => ui.UpdateHappinessDisplay(happiness, _isHovered);
        OnSickChanged -= (isSick) => {
            Debug.Log($"Monster sick status changed: {isSick}");
        };
        OnHoverChanged -= (hovered) => {
            ui.UpdateHungerDisplay(currentHunger, hovered);
            ui.UpdateHappinessDisplay(currentHappiness, hovered);
        };
    }

    private void StartCoroutines()
    {
        float goldCoinInterval = (float)TimeSpan.FromMinutes((double)CoinType.Gold).TotalSeconds;
        float silverCoinInterval = (float)TimeSpan.FromMinutes((double)CoinType.Silver).TotalSeconds;
        // float poopInterval = (float)TimeSpan.FromMinutes(20).TotalSeconds;
        float poopInterval = 20f;

        _hungerCoroutine = StartCoroutine(HungerRoutine(1f));
        _happinessCoroutine = StartCoroutine(HappinessRoutine(1f));
        _poopCoroutine = StartCoroutine(PoopRoutine(poopInterval));
        _goldCoinCoroutine = StartCoroutine(CoinCoroutine(goldCoinInterval, CoinType.Gold));
        _silverCoinCoroutine = StartCoroutine(CoinCoroutine(silverCoinInterval, CoinType.Silver));
    }

    private void StopManagedCoroutines()
    {
        if (_hungerCoroutine != null) StopCoroutine(_hungerCoroutine);
        if (_happinessCoroutine != null) StopCoroutine(_happinessCoroutine);
        if (_poopCoroutine != null) StopCoroutine(_poopCoroutine);
        if (_goldCoinCoroutine != null) StopCoroutine(_goldCoinCoroutine);
        if (_silverCoinCoroutine != null) StopCoroutine(_silverCoinCoroutine);
    }

    private void HandleMovement()
    {
        if (_stateMachine?.CurrentState == MonsterState.Eating)
        {
            return;
        }
        
        if (_foodHandler?.IsEating == true)
        {
            return;
        }
        
        bool isMovementState = _stateMachine?.CurrentState == MonsterState.Walking || 
                              _stateMachine?.CurrentState == MonsterState.Running;
        
        if (isMovementState && _foodHandler?.NearestFood == null)
        {
            _foodHandler?.FindNearestFood();
        }
        
        if (isMovementState && _foodHandler?.NearestFood != null)
        {
            _foodHandler?.HandleFoodLogic(ref _targetPosition);
        }
        
        // Apply separation behavior to target position
        if (isMovementState)
        {
            _targetPosition = _separationBehavior.ApplySeparationToTarget(_targetPosition);
        }

        Vector2 oldPosition = _rectTransform.anchoredPosition;
        _movementHandler.UpdateMovement(ref _targetPosition, stats);

        // Check if monster moved significantly and request immediate depth sort
        Vector2 newPosition = _rectTransform.anchoredPosition;
        if (Vector2.Distance(newPosition, _lastSortPosition) >= _depthSortThreshold)
        {
            _lastSortPosition = newPosition;
        }

        bool isPursuingFood = _foodHandler?.NearestFood != null;
        float distanceToTarget = Vector2.Distance(_rectTransform.anchoredPosition, _targetPosition);
        if (distanceToTarget < 10f && !isPursuingFood && isMovementState)
        {
            SetRandomTarget();
        }
    }

    public void TriggerEating() 
    {
        _stateMachine?.ForceState(MonsterState.Eating);
    }

    public void SetRandomTarget()
    {
        _targetPosition = _movementBounds?.GetRandomTarget() ?? Vector2.zero;
    }

    public void SetHunger(float value)
    {
        if (Mathf.Approximately(_currentHunger, value)) return;
        _currentHunger = value;
        OnHungerChanged?.Invoke(_currentHunger);
    }

    public void SetHappiness(float value)
    {
        if (Mathf.Approximately(_currentHappiness, value)) return;
        _currentHappiness = value;
        OnHappinessChanged?.Invoke(_currentHappiness);
    }

    public void SetHovered(bool value)
    {
        if (_isHovered == value) return;
        _isHovered = value;
        OnHoverChanged?.Invoke(_isHovered);
    }

    public void DropCoinAfterPoke() => DropCoin(CoinType.Silver);

    public void UpdateVisuals() => _visualHandler?.UpdateMonsterVisuals();

    public void Feed(float amount)
    {
        // Sick monsters can't gain hunger from regular food
        if (!_isSick)
        {
            SetHunger(Mathf.Clamp(currentHunger + amount, 0f, 100f));
            IncreaseHappiness(amount);
            _evolutionHandler?.OnFoodConsumed();
        }
        else
        {
            Debug.Log($"{gameObject.name} is too sick to eat regular food!");
        }
    }

    public void IncreaseHappiness(float amount)
    {
        SetHappiness(Mathf.Clamp(currentHappiness + amount, 0f, 100f));
    }

    private void UpdateHappinessBasedOnArea()
    {
        var gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager?.gameArea == null) return;

        float gameAreaHeight = gameManager.gameArea.sizeDelta.y;
        float screenHeight = Screen.currentResolution.height;
        float heightRatio = gameAreaHeight / screenHeight;

        if (heightRatio >= 0.5f)
            SetHappiness(Mathf.Clamp(currentHappiness + stats.areaHappinessRate, 0f, 100f));
        else
            SetHappiness(Mathf.Clamp(currentHappiness - stats.areaHappinessRate, 0f, 100f));
    }

    private void Poop() => ServiceLocator.Get<GameManager>().SpawnPoopAt(_rectTransform.anchoredPosition);
    private void DropCoin(CoinType type) => ServiceLocator.Get<GameManager>().SpawnCoinAt(_rectTransform.anchoredPosition, type);

    public void SetSick(bool value)
    {
        if (_isSick == value) return;
        _isSick = value;
        OnSickChanged?.Invoke(_isSick);
        
        if (_isSick)
        {
            Debug.Log($"{gameObject.name} became sick!");
            // Could trigger visual changes, different animations, etc.
        }
        else
        {
            Debug.Log($"{gameObject.name} recovered from sickness!");
        }
    }

    public void TreatSickness()
    {
        if (!_isSick) return;
        
        // Reset to healthy state
        SetSick(false);
        SetHunger(50f); // Override to 50% hunger
        SetHappiness(10f); // Override to 10% happiness
        _lowHungerTime = 0f; // Reset low hunger timer
        
        Debug.Log($"{gameObject.name} has been treated and is now healthy!");
    }

    public void GiveMedicine()
    {
        if (_isSick)
        {
            TreatSickness();
            // Could consume medicine item, cost coins, etc.
        }
    }

    public void OnPointerEnter(PointerEventData e) => _interactionHandler?.OnPointerEnter(e);
    public void OnPointerExit(PointerEventData e) => _interactionHandler?.OnPointerExit(e);
    public void OnPointerClick(PointerEventData eventData)    {
        _interactionHandler?.OnPointerClick(eventData);
        _evolutionHandler?.OnInteraction();
    }

    public void SaveMonData() => _saveHandler?.SaveData();
    public void LoadMonData() => _saveHandler?.LoadData();    public void SetMonsterData(MonsterDataSO newMonsterData)
    {
        if (newMonsterData == null) return;
        
        monsterData = newMonsterData;
        
        if (monsterID.StartsWith("temp_") || string.IsNullOrEmpty(monsterID))
        {
            monsterID = $"{monsterData.id}_Lv{evolutionLevel}_{System.Guid.NewGuid().ToString("N")[..8]}";
            gameObject.name = $"{monsterData.monsterName}_{monsterID}";
        }
        
        if (_evolutionHandler == null)
        {
            _evolutionHandler = new MonsterEvolutionHandler(this);
        }
        else
        {
            _evolutionHandler.InitializeWithMonsterData();
        }
        
        if (_visualHandler != null)
        {
            _visualHandler.ApplyMonsterVisuals();
        }
        
        _saveHandler?.LoadData();
    }

    public void ForceResetEating()
    {
        _foodHandler?.ForceResetEating();
    }    // Add public methods to access evolution handler
    public float GetEvolutionProgress() => _evolutionHandler?.GetEvolutionProgress() ?? 0f;
    public void ForceEvolution() => _evolutionHandler?.ForceEvolution();

    // Add these getter methods for save handler to access evolution data
    public float GetEvolutionTimeSinceCreation() 
    {
        float time = _evolutionHandler?.TimeSinceCreation ?? 0f;
        return time;
    }
    
    public int GetEvolutionFoodConsumed() 
    {
        int food = _evolutionHandler?.FoodConsumed ?? 0;
        return food;
    }
    
    public int GetEvolutionInteractionCount() 
    {
        int interactions = _evolutionHandler?.InteractionCount ?? 0;
        return interactions;
    }

    public void LoadEvolutionData(float timeSinceCreation, int foodConsumed, int interactionCount)
    {
        _evolutionHandler?.LoadEvolutionData(timeSinceCreation, foodConsumed, interactionCount);
    }

    private IEnumerator HungerRoutine(float interval)
    {
        while (true)
        {
            SetHunger(Mathf.Clamp(currentHunger - stats.hungerDepletionRate, 0f, 100f));
            
            // Track low hunger time for sickness
            if (currentHunger <= SICK_HUNGER_THRESHOLD && !_isSick)
            {
                _lowHungerTime += interval;
                
                // Check if monster should become sick
                if (_lowHungerTime >= SICK_THRESHOLD_TIME)
                {
                    SetSick(true);
                }
            }
            else if (currentHunger > SICK_HUNGER_THRESHOLD)
            {
                // Reset timer if hunger goes above threshold
                _lowHungerTime = 0f;
            }
            
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator HappinessRoutine(float interval)
    {
        while (true)
        {
            // Skip normal happiness updates if sick
            if (!_isSick)
            {
                UpdateHappinessBasedOnArea();
                
                // Drain happiness if hunger is below threshold
                if (currentHunger < monsterData.hungerHappinessThreshold)
                {
                    SetHappiness(Mathf.Clamp(currentHappiness - monsterData.hungerHappinessDrainRate, 0f, 100f));
                }
            }
            else
            {
                // Sick monsters lose happiness faster
                SetHappiness(Mathf.Clamp(currentHappiness - 1f, 0f, 100f)); // Drain 1 happiness per second when sick
            }
            
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator PoopRoutine(float interval)
    {
        yield return new WaitForSeconds(interval);
        while (true)
        {
            Poop();
            yield return new WaitForSeconds(interval);
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

    public float GetLowHungerTime() => _lowHungerTime;

    public void SetLowHungerTime(float value) => _lowHungerTime = value;
}