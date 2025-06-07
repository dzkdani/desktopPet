using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs & Pool Settings")]
    public GameObject monsterPrefab;
    public GameObject foodPrefab;
    public GameObject poopPrefab;
    public GameObject coinPrefab;
    public RectTransform poolContainer;
    public int initialPoolSize = 20;

    [Header("Game Settings")]
    public RectTransform gameArea;
    public Canvas mainCanvas;
    public MonsterDatabaseSO monsterDatabase;

    [Header("Food Placement Settings")]
    public GameObject foodPlacementIndicator;
    public Color validPositionColor = Color.green;
    public Color invalidPositionColor = Color.red;

    [Header("Rendering Settings")]
    public bool enableDepthSorting = true;
    private float lastSortTime = 0f;
    private float sortInterval = 0.1f;
    
    // Cache frequently accessed components
    private Camera _mainCamera;
    private RectTransform _foodIndicatorRT;
    private Image _foodIndicatorImage;
    
    private Queue<GameObject> _foodPool = new Queue<GameObject>();
    private Queue<GameObject> _poopPool = new Queue<GameObject>();
    private Queue<GameObject> _coinPool = new Queue<GameObject>();

    [HideInInspector] public int poopCollected;
    [HideInInspector] public int coinCollected;
    [HideInInspector] public List<MonsterController> activeMonsters = new List<MonsterController>();
    public List<FoodController> activeFoods = new List<FoodController>();
    private List<string> savedMonIDs = new List<string>();

    private bool isInPlacementMode = false;
    private int pendingFoodCost = 0;

    private void Awake()
    {
        ServiceLocator.Register(this);
        InitializePools();
        CacheComponents();
    }
    
    private void CacheComponents()
    {
        _mainCamera = mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCanvas.worldCamera;
        _foodIndicatorRT = foodPlacementIndicator.GetComponent<RectTransform>();
        _foodIndicatorImage = foodPlacementIndicator.GetComponent<Image>();
    }

    void Start() => LoadGame();

    void Update()
    {
        if (isInPlacementMode)
        {
            IndicatorPlacementHandler();
            FoodPlacementHandler();
        }

        // Add depth sorting for monsters
        if (enableDepthSorting && Time.time - lastSortTime >= sortInterval)
        {
            SortMonstersByDepth();
            lastSortTime = Time.time;
        }
    }

    private void InitializePools()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePoolObject(foodPrefab, _foodPool);
            CreatePoolObject(poopPrefab, _poopPool);
            CreatePoolObject(coinPrefab, _coinPool);
        }
    }

    private void CreatePoolObject(GameObject prefab, Queue<GameObject> pool)
    {
        var obj = Instantiate(prefab, poolContainer);
        obj.SetActive(false);
        pool.Enqueue(obj);
    }

    // Simplify LoadGame method
    private void LoadGame()
    {
        coinCollected = SaveSystem.LoadCoin();
        poopCollected = SaveSystem.LoadPoop();
        savedMonIDs = SaveSystem.LoadSavedMonIDs();

        foreach (var id in savedMonIDs)
        {
            if (SaveSystem.LoadMon(id, out _)) // Don't need the data variable
            {
                var (monsterData, _) = GetMonsterDataAndLevelFromID(id);
                SpawnMonster(monsterData, id); // Direct call instead of wrapper
            }
        }
    }

    public void SaveAllMons()
    {
        foreach (var monster in activeMonsters)
        {
            var saveData = new MonsterSaveData
            {
                monsterId = monster.monsterID,
                lastHunger = monster.currentHunger,
                lastHappiness = monster.currentHappiness,
                isFinalForm = monster.isFinalForm,
                evolutionLevel = monster.evolutionLevel,
                timeSinceCreation = monster.GetEvolutionTimeSinceCreation(),
                foodConsumed = monster.GetEvolutionFoodConsumed(),
                interactionCount = monster.GetEvolutionInteractionCount()
            };
            SaveSystem.SaveMon(saveData);
        }

        SaveSystem.SaveMonIDs(savedMonIDs);
        SaveSystem.Flush();
    }

    private void SaveGameData()
    {
        SaveAllMons();
        SaveSystem.SavePoop(poopCollected);
        SaveSystem.SaveCoin(coinCollected);
        SaveSystem.Flush();
    }

    public void SpawnMonster(MonsterDataSO monsterData = null, string existingID = null)
    {
        GameObject monster = CreateMonster(monsterData);
        
        var controller = monster.GetComponent<MonsterController>();

        if (!string.IsNullOrEmpty(existingID))
        {
            controller.monsterID = existingID;
            var (_, evolutionLevel) = GetMonsterDataAndLevelFromID(existingID);
            if (evolutionLevel > 0) controller.evolutionLevel = evolutionLevel;
        }
        else
        {
            var data = controller.MonsterData;
            controller.monsterID = $"{data.id}_Lv{controller.evolutionLevel}_{System.Guid.NewGuid().ToString("N")[..8]}";
        }

        controller.LoadMonData();

        var finalData = controller.MonsterData;
        monster.name = $"{finalData.monsterName}_{controller.monsterID}";

        if (string.IsNullOrEmpty(existingID))
            RegisterMonster(controller);
        else
            RegisterActiveMonster(controller);
    }

    public void BuyMons(int cost = 10)
    {
        if (SpentCoin(cost)) SpawnMonster();
    }


    private (MonsterDataSO, int) GetMonsterDataAndLevelFromID(string monsterID)
    {
        var parts = monsterID.Split('_');
        if (parts.Length >= 2)
        {
            string monsterTypeId = parts[0];
            string levelPart = parts[1];
            int evolutionLevel = 0;
            
            if (levelPart.StartsWith("Lv"))
            {
                if (!int.TryParse(levelPart.Substring(2), out evolutionLevel))
                {
                    evolutionLevel = 0;
                }
            }
            
            foreach (var data in monsterDatabase.monsters)
            {
                if (data.id == monsterTypeId)
                {
                    return (data, evolutionLevel);
                }
            }
        }
        
        return (null, 0);
    }

    private GameObject CreateMonster(MonsterDataSO monsterData = null)
    {
        var monster = Instantiate(monsterPrefab, gameArea);
        
        // Get proper movement bounds that account for monster size
        var monsterController = monster.GetComponent<MonsterController>();
        var rectTransform = monster.GetComponent<RectTransform>();
        var movementBounds = new MonsterMovementBounds(rectTransform, this);
        
        // Use the same bounds calculation as movement
        Vector2 spawnPosition = movementBounds.GetRandomTarget();
        monster.transform.localPosition = spawnPosition;

        if (monsterController != null)
        {
            MonsterDataSO dataToUse = monsterData;
            
            // If no data provided, pick random from database
            if (dataToUse == null && monsterDatabase != null && monsterDatabase.monsters.Count > 0)
            {
                dataToUse = monsterDatabase.monsters[UnityEngine.Random.Range(0, monsterDatabase.monsters.Count)];
            }
            
            if (dataToUse != null)
            {
                monsterController.SetMonsterData(dataToUse);
                monster.name = $"{dataToUse.monsterName}_Temp";
            }
            else
            {
                monster.name = "Monster_Temp";
            }
        }
        else
        {
            monster.name = "Monster_Temp";
        }

        return monster;
    }

    private void RegisterMonster(MonsterController monsterController)
    {
        savedMonIDs.Add(monsterController.monsterID);
        SaveSystem.SaveMonIDs(savedMonIDs);
        RegisterActiveMonster(monsterController);
    }

    public void RegisterActiveMonster(MonsterController monster)
    {
        if (!activeMonsters.Contains(monster))
        {
            activeMonsters.Add(monster);
        }
    }

    public void RemoveSavedMonsterID(string monsterID)
    {
        savedMonIDs.Remove(monsterID);
        SaveSystem.SaveMonIDs(savedMonIDs);
    }

    public void AddSavedMonsterID(string monsterID)
    {
        if (!savedMonIDs.Contains(monsterID))
        {
            savedMonIDs.Add(monsterID);
            SaveSystem.SaveMonIDs(savedMonIDs);
        }
    }

    public void StartFoodPurchase(int cost)
    {
        if (coinCollected >= cost)
        {
            pendingFoodCost = cost;
            isInPlacementMode = true;
            foodPlacementIndicator.SetActive(true);
            ServiceLocator.Get<UIManager>().ShowMessage("Click to place food (Right-click to cancel)");
        }
        else
        {
            ServiceLocator.Get<UIManager>().ShowMessage("Not enough coins!", 1f);
        }
    }

    private Vector2 ScreenToGameAreaPosition()
    {
        var cam = mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gameArea, Input.mousePosition, cam, out Vector2 localPoint);
        
        return localPoint;
    }

    private void IndicatorPlacementHandler()
    {
        var indicatorPos = ScreenToGameAreaPosition();
        
        if (_foodIndicatorRT.parent != gameArea)
            _foodIndicatorRT.SetParent(gameArea, false);
        
        _foodIndicatorRT.anchoredPosition = indicatorPos;
        _foodIndicatorImage.color = IsPositionInGameArea(indicatorPos) ? validPositionColor : invalidPositionColor;
    }

    private void FoodPlacementHandler()
    {
        if (Input.GetMouseButtonDown(0)) TryPlaceFood();
        else if (Input.GetMouseButtonDown(1)) CancelPlacement();
    }

    private void TryPlaceFood()
    {
        Vector2 position = ScreenToGameAreaPosition();

        if (IsPositionInGameArea(position))
        {
            if (SpentCoin(pendingFoodCost))
            {
                SpawnFoodAtPosition(position);
            }
            EndPlacement();
        }
        else
        {
            ServiceLocator.Get<UIManager>().ShowMessage("Can't place here!", 1f);
        }
    }

    private void CancelPlacement()
    {
        EndPlacement();
        ServiceLocator.Get<UIManager>().ShowMessage("Placement canceled", 1f);
    }

    private void EndPlacement()
    {
        isInPlacementMode = false;
        pendingFoodCost = 0;
        foodPlacementIndicator.SetActive(false);
    }

    private void SpawnFoodAtPosition(Vector2 position)
    {
        var food = GetPooledObject(_foodPool, foodPrefab);
        SetupPooledObject(food, gameArea, position);

        var foodController = food.GetComponent<FoodController>();
        if (foodController != null && !activeFoods.Contains(foodController))
        {
            activeFoods.Add(foodController);
        }
    }

    public GameObject SpawnCoinAt(Vector2 anchoredPos, CoinType type)
    {
        var coin = GetPooledObject(_coinPool, coinPrefab);
        SetupPooledObject(coin, gameArea, anchoredPos);

        coin.GetComponent<CoinController>().Initialize(type);
        return coin;
    }

    public GameObject SpawnPoopAt(Vector2 anchoredPos)
    {
        var poop = GetPooledObject(_poopPool, poopPrefab);
        SetupPooledObject(poop, gameArea, anchoredPos);
        return poop;
    }

    private GameObject GetPooledObject(Queue<GameObject> pool, GameObject prefab)
    {
        return pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab, poolContainer);
    }

    private void SetupPooledObject(GameObject obj, RectTransform parent, Vector2 position)
    {
        var rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        var indicatorRT = foodPlacementIndicator.GetComponent<RectTransform>();
        rt.anchorMin = indicatorRT.anchorMin;
        rt.anchorMax = indicatorRT.anchorMax;
        rt.pivot = indicatorRT.pivot;

        rt.anchoredPosition = position;
        obj.SetActive(true);
    }

    public void DespawnToPool(GameObject obj)
    {
        obj.transform.SetParent(poolContainer, false);
        obj.SetActive(false);

        // More reliable than name checking
        if (obj.TryGetComponent<FoodController>(out var food))
        {
            _foodPool.Enqueue(obj);
            activeFoods.Remove(food);
        }
        else if (obj.TryGetComponent<CoinController>(out _))
            _coinPool.Enqueue(obj);
        else if (obj.name.Contains("Poop"))
        {
            _poopPool.Enqueue(obj);
            CollectPoop();
        } 
    }

    public System.Action<int> OnCoinChanged;
    public System.Action<int> OnPoopChanged;
    
    public bool SpentCoin(int amount)
    {
        if (coinCollected < amount) return false;

        coinCollected -= amount;
        SaveSystem.SaveCoin(coinCollected);
        OnCoinChanged?.Invoke(coinCollected); 
        return true;
    }

    public void CollectPoop(int amount = 1)
    {
        poopCollected += amount;
        SaveSystem.SavePoop(poopCollected);
        OnPoopChanged?.Invoke(poopCollected);
    }

    public bool IsPositionInGameArea(Vector2 localPosition)
    {
        var rect = gameArea.rect;
        return localPosition.x >= rect.xMin && localPosition.x <= rect.xMax &&
               localPosition.y >= rect.yMin && localPosition.y <= rect.yMax;
    }

    void OnApplicationQuit() => SaveGameData();
    void OnApplicationPause(bool pauseStatus) { if (pauseStatus) SaveGameData(); }

#if UNITY_EDITOR
    void OnDisable() { if (Application.isPlaying) SaveGameData(); }
#endif

    void OnDestroy() => ServiceLocator.Unregister<GameManager>();

    void OnDrawGizmosSelected()
    {
        if (gameArea != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(gameArea.anchoredPosition,
                new Vector3(gameArea.rect.width, gameArea.rect.height, 0));
        }
    }

    public void SortMonstersByDepth()
    {
        if (activeMonsters.Count <= 1) return;

        // Use array for better performance with large lists
        var validMonsters = new List<MonsterController>(activeMonsters.Count);
        
        // Filter out null/inactive monsters first
        for (int i = activeMonsters.Count - 1; i >= 0; i--)
        {
            var monster = activeMonsters[i];
            if (monster == null || !monster.gameObject.activeInHierarchy)
            {
                activeMonsters.RemoveAt(i); // Clean up invalid references
            }
            else
            {
                validMonsters.Add(monster);
            }
        }

        // Sort by Y position (higher Y = lower sibling index)
        validMonsters.Sort((a, b) => 
            b.transform.position.y.CompareTo(a.transform.position.y));

        // Update sibling indices
        for (int i = 0; i < validMonsters.Count; i++)
        {
            validMonsters[i].transform.SetSiblingIndex(i);
        }
    }
}