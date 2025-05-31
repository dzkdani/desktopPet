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
    
    // Object Pools
    private Queue<GameObject> _foodPool = new Queue<GameObject>();
    private Queue<GameObject> _poopPool = new Queue<GameObject>();
    private Queue<GameObject> _coinPool = new Queue<GameObject>();
    
    // Game State
    [HideInInspector] public int poopCollected;
    [HideInInspector] public int coinCollected;
    [HideInInspector] public List<MonsterController> activeMonsters = new List<MonsterController>();
    public List<FoodController> activeFoods = new List<FoodController>();
    private List<string> savedMonIDs = new List<string>();
    
    // Food Placement
    private bool isInPlacementMode = false;
    private int pendingFoodCost = 0;
    
    private void Awake()
    {
        ServiceLocator.Register(this);
        InitializePools();
    }

    void Start() => LoadGame();

    void Update()
    {
        if (isInPlacementMode)
        {
            IndicatorPlacementHandler();
            FoodPlacementHandler();
        }
    }

    #region Initialization
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
    #endregion

    #region Save/Load System
    private void LoadGame()
    {
        coinCollected = SaveSystem.LoadCoin();
        poopCollected = SaveSystem.LoadPoop();
        savedMonIDs = SaveSystem.LoadSavedMonIDs();
        
        foreach (var id in savedMonIDs)
        {
            if (SaveSystem.LoadMon(id, out var data))
            {
                SpawnLoadedMons(id);
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
                lastHappiness = monster.currentHappiness, // Save happiness
                isEvolved = monster.isEvolved,
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
    #endregion

    #region Monster Management
    public void BuyMons(int cost = 10)
    {
        if (SpentCoin(cost))
        {
            SpawnMonFromShop();
        }
    }

    private void SpawnMonFromShop()
    {
        var monster = CreateMonster();
        var monsterController = monster.GetComponent<MonsterController>();
        
        // Generate save ID format
        var monsterData = monsterController.MonsterData;
        if (monsterData != null)
        {
            monsterController.monsterID = $"{monsterData.id}_Lv{monsterController.evolutionLevel}_{System.Guid.NewGuid().ToString("N")[..8]}";
            
            // Rename GameObject with monster name + save ID
            monster.name = $"{monsterData.monsterName}_{monsterController.monsterID}";
        }
        else
        {
            monsterController.monsterID = System.Guid.NewGuid().ToString();
            monster.name = $"Monster_{monsterController.monsterID}";
        }
        
        monsterController.LoadMonData();
        RegisterMonster(monsterController);
    }

    private void SpawnLoadedMons(string monID)
    {
        var monster = CreateMonster();
        var monsterController = monster.GetComponent<MonsterController>();
        
        monsterController.monsterID = monID;
        
        // Rename GameObject with monster name + save ID
        var monsterData = monsterController.MonsterData;
        if (monsterData != null)
        {
            monster.name = $"{monsterData.monsterName}_{monID}";
        }
        else
        {
            monster.name = $"Monster_{monID}";
        }
        
        monsterController.LoadMonData();
        RegisterActiveMonster(monsterController);
    }

    public void SpawnLoadedMonsViaGacha(string monID)
    {
        var monster = CreateMonster();
        var monsterController = monster.GetComponent<MonsterController>();
        
        if (string.IsNullOrEmpty(monID))
        {
            var monsterData = monsterController.MonsterData;
            if (monsterData != null)
            {
                monID = $"{monsterData.id}_Lv{monsterController.evolutionLevel}_{System.Guid.NewGuid().ToString("N")[..8]}";
            }
            else
            {
                monID = System.Guid.NewGuid().ToString();
            }
        }
        
        monsterController.monsterID = monID;
        
        // Rename GameObject with monster name + save ID
        var data = monsterController.MonsterData;
        if (data != null)
        {
            monster.name = $"{data.monsterName}_{monID}";
        }
        else
        {
            monster.name = $"Monster_{monID}";
        }
        
        monsterController.LoadMonData();
        RegisterMonster(monsterController);
    }

    public void SpawnMonsterFromGacha(MonsterDataSO monsterData)
    {
        var monster = CreateMonsterByData(monsterData);
        var monsterController = monster.GetComponent<MonsterController>();
        
        // Generate save ID format with monster data
        monsterController.monsterID = $"{monsterData.id}_Lv{monsterController.evolutionLevel}_{System.Guid.NewGuid().ToString("N")[..8]}";
        
        // Rename GameObject with monster name + save ID
        monster.name = $"{monsterData.monsterName}_{monsterController.monsterID}";
        
        monsterController.LoadMonData();
        RegisterMonster(monsterController);
    }

    private GameObject CreateMonster()
    {
        var monster = Instantiate(monsterPrefab, gameArea);
        var bounds = gameArea.rect;
        
        monster.transform.localPosition = new Vector2(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            bounds.min.y + 20f
        );
        
        var monsterController = monster.GetComponent<MonsterController>();
        if (monsterController != null && monsterDatabase != null && monsterDatabase.monsters.Count > 0)
        {
            var randomMonsterData = monsterDatabase.monsters[UnityEngine.Random.Range(0, monsterDatabase.monsters.Count)];
            // Set monster data BEFORE other initialization
            monsterController.SetMonsterData(randomMonsterData);
            
            // Set initial name (will be updated with proper ID later)
            monster.name = $"{randomMonsterData.monsterName}_Temp";
        }
        else
        {
            monster.name = "Monster_Temp";
        }
        
        return monster;
    }

    private GameObject CreateMonsterByData(MonsterDataSO monsterData)
    {
        var monster = Instantiate(monsterPrefab, gameArea);
        var bounds = gameArea.rect;
        
        monster.transform.localPosition = new Vector2(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            bounds.min.y + 20f
        );
        
        var monsterController = monster.GetComponent<MonsterController>();
        if (monsterController != null)
        {
            // Set monster data IMMEDIATELY after instantiation
            monsterController.SetMonsterData(monsterData);
            
            // Set initial name (will be updated with proper ID later)
            monster.name = $"{monsterData.monsterName}_Temp";
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
    #endregion

    #region Food System
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

    private Vector2 ScreenToCanvasPosition()
    {
        var canvasRect = mainCanvas.GetComponent<RectTransform>();
        var cam = mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, Input.mousePosition, cam, out Vector2 localPoint);

        return localPoint;
    }

    private void IndicatorPlacementHandler()
    {
        var indicatorPos = ScreenToGameAreaPosition();
        var indicatorRT = foodPlacementIndicator.GetComponent<RectTransform>();
        
        // Ensure indicator is child of game area for consistent positioning
        if (indicatorRT.parent != gameArea)
        {
            indicatorRT.SetParent(gameArea, false);
        }
        
        indicatorRT.anchoredPosition = indicatorPos;

        bool isValid = IsPositionInGameArea(indicatorPos);
        foodPlacementIndicator.GetComponent<Image>().color = isValid ? validPositionColor : invalidPositionColor;
    }

    private void FoodPlacementHandler()
    {
        if (Input.GetMouseButtonDown(0)) TryPlaceFood();
        else if (Input.GetMouseButtonDown(1)) CancelPlacement();
    }

    private void TryPlaceFood()
    {
        // Use the same coordinate conversion as the indicator
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

    #endregion

    #region Object Spawning
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
        
        // Reset all transform properties to ensure consistent behavior
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        
        // Match the indicator's anchor and pivot settings exactly
        var indicatorRT = foodPlacementIndicator.GetComponent<RectTransform>();
        rt.anchorMin = indicatorRT.anchorMin;
        rt.anchorMax = indicatorRT.anchorMax;
        rt.pivot = indicatorRT.pivot;
        
        rt.anchoredPosition = position;
        obj.SetActive(true);
    }

    public void DespawnPools(GameObject obj)
    {
        obj.transform.SetParent(poolContainer, false);
        obj.SetActive(false);

        if (obj.name.Contains("Poop")) _poopPool.Enqueue(obj);
        else if (obj.name.Contains("Coin")) _coinPool.Enqueue(obj);
        else if (obj.name.Contains("Food")) _foodPool.Enqueue(obj);
    }
    #endregion

    #region Utility
    public bool SpentCoin(int amount)
    {
        if (coinCollected < amount) return false;
        
        coinCollected -= amount;
        SaveSystem.SaveCoin(coinCollected);
        ServiceLocator.Get<UIManager>().UpdateCoinCounter();
        return true;
    }

    public bool IsPositionInGameArea(Vector2 localPosition)
    {
        var rect = gameArea.rect;
        return localPosition.x >= rect.xMin && localPosition.x <= rect.xMax &&
               localPosition.y >= rect.yMin && localPosition.y <= rect.yMax;
    }

    public static Vector2 ScreenToCanvasPosition(Canvas canvas)
    {
        var canvasRect = canvas.GetComponent<RectTransform>();
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, Input.mousePosition, cam, out Vector2 localPoint);

        return localPoint;
    }
    #endregion

    #region Application Lifecycle
    void OnApplicationQuit() => SaveGameData();
    void OnApplicationPause(bool pauseStatus) { if (pauseStatus) SaveGameData(); }

#if UNITY_EDITOR
    void OnDisable() { if (Application.isPlaying) SaveGameData(); }
#endif

    void OnDestroy() => ServiceLocator.Unregister<GameManager>();
    #endregion

    #region Debug
    void OnDrawGizmosSelected() 
    {
        if (gameArea != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(gameArea.anchoredPosition, 
                new Vector3(gameArea.rect.width, gameArea.rect.height, 0));
        }
    }
    #endregion
}