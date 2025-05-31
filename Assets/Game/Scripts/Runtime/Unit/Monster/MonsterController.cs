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
    public bool isEvolved;
    public bool isFinalForm;
    public int evolutionLevel;
    public MonsterDataSO MonsterData => monsterData;

    public float currentHunger => _currentHunger;
    public float currentHappiness => _currentHappiness;
    public bool isHovered => _isHovered;
    public bool IsLoaded => _isLoaded;
    public FoodController nearestFood => _foodHandler?.NearestFood;

    public event Action<float> OnHungerChanged;
    public event Action<float> OnHappinessChanged;
    public event Action<bool> OnHoverChanged;

    // Modular components
    private MonsterSaveHandler _saveHandler;
    private MonsterVisualHandler _visualHandler;
    private MonsterFoodHandler _foodHandler;
    private MonsterInteractionHandler _interactionHandler;
    private MonsterMovementBounds _movementBounds;

    // Core components
    private SkeletonGraphic _monsterSpineGraphic;
    private RectTransform _rectTransform;
    private GameManager _gameManager;
    private MonsterStateMachine _stateMachine;
    private MonsterMovement _movementHandler;

    // Coroutine references for management
    private Coroutine _hungerCoroutine;
    private Coroutine _happinessCoroutine;
    private Coroutine _poopCoroutine;
    private Coroutine _goldCoinCoroutine;
    private Coroutine _silverCoinCoroutine;

    private Vector2 _targetPosition;
    private bool _isLoaded = false;
    private bool _shouldDropCoinAfterPoke = false;
    private float _currentHunger = 100f;
    private float _currentHappiness = 100f;
    private bool _isHovered;

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
        HandleMovement();
    }

    private void InitializeModules()
    {
        _saveHandler = new MonsterSaveHandler(this);
        _visualHandler = new MonsterVisualHandler(this, _monsterSpineGraphic);
        _interactionHandler = new MonsterInteractionHandler(this, _stateMachine);
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
    }

    private void SubscribeToEvents()
    {
        // Update text continuously but don't adjust positions
        OnHungerChanged += (hunger) => ui.UpdateHungerDisplay(hunger, _isHovered);
        OnHappinessChanged += (happiness) => ui.UpdateHappinessDisplay(happiness, _isHovered);
        
        // Control visibility on hover
        OnHoverChanged += (hovered) => {
            ui.UpdateHungerDisplay(currentHunger, hovered);
            ui.UpdateHappinessDisplay(currentHappiness, hovered);
        };
    }

    private void UnsubscribeFromEvents()
    {
        // Update to match the new subscribe events
        OnHungerChanged -= (hunger) => ui.UpdateHungerDisplay(hunger, _isHovered);
        OnHappinessChanged -= (happiness) => ui.UpdateHappinessDisplay(happiness, _isHovered);
        OnHoverChanged -= (hovered) => {
            ui.UpdateHungerDisplay(currentHunger, hovered);
            ui.UpdateHappinessDisplay(currentHappiness, hovered);
        };
    }

    private void StartCoroutines()
    {
        float goldCoinInterval = (float)TimeSpan.FromMinutes((double)CoinType.Gold).TotalSeconds;
        float silverCoinInterval = (float)TimeSpan.FromMinutes((double)CoinType.Silver).TotalSeconds;
        float poopInterval = (float)TimeSpan.FromMinutes(20).TotalSeconds;

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

        _movementHandler.UpdateMovement(ref _targetPosition, stats);

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

    public void SetShouldDropCoinAfterPoke(bool value) => _shouldDropCoinAfterPoke = value;

    public void UpdateVisuals() => _visualHandler?.UpdateMonsterVisuals();

    public void Feed(float amount)
    {
        float oldHunger = currentHunger;
        SetHunger(Mathf.Clamp(currentHunger + amount, 0f, 100f));
        IncreaseHappiness(amount);
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

    public void OnPointerEnter(PointerEventData e) => _interactionHandler?.OnPointerEnter(e);
    public void OnPointerExit(PointerEventData e) => _interactionHandler?.OnPointerExit(e);
    public void OnPointerClick(PointerEventData eventData) => _interactionHandler?.OnPointerClick(eventData);

    public void SaveMonData() => _saveHandler?.SaveData();
    public void LoadMonData() => _saveHandler?.LoadData();

    public void SetMonsterData(MonsterDataSO newMonsterData)
    {
        if (newMonsterData == null) return;
        
        monsterData = newMonsterData;
        
        if (monsterID.StartsWith("temp_") || string.IsNullOrEmpty(monsterID))
        {
            monsterID = $"{monsterData.id}_Lv{evolutionLevel}_{System.Guid.NewGuid().ToString("N")[..8]}";
            gameObject.name = $"{monsterData.monsterName}_{monsterID}";
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
    }

    private IEnumerator HungerRoutine(float interval)
    {
        while (true)
        {
            SetHunger(Mathf.Clamp(currentHunger - stats.hungerDepletionRate, 0f, 100f));
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator HappinessRoutine(float interval)
    {
        while (true)
        {
            UpdateHappinessBasedOnArea();
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
}