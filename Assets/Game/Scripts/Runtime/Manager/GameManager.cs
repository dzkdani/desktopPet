using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs & Pool Settings")]
    public GameObject monPrefab;
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
            UpdateFoodPlacement();
            HandleFoodPlacementInput();
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
        
        monsterController.monsterID = System.Guid.NewGuid().ToString();
        monsterController.LoadMonData();
        
        RegisterMonster(monsterController);
    }

    private void SpawnLoadedMons(string monID)
    {
        var monster = CreateMonster();
        var monsterController = monster.GetComponent<MonsterController>();
        
        monsterController.monsterID = monID;
        monsterController.LoadMonData();
        RegisterToActiveMons(monsterController);
    }

    public void SpawnLoadedMonsViaGacha(string monID)
    {
        var monster = CreateMonster();
        var monsterController = monster.GetComponent<MonsterController>();
        
        if (string.IsNullOrEmpty(monID))
        {
            monID = System.Guid.NewGuid().ToString();
        }
        
        monsterController.monsterID = monID;
        monsterController.LoadMonData();
        
        RegisterMonster(monsterController);
    }

    private GameObject CreateMonster()
    {
        var monster = Instantiate(monPrefab, gameArea);
        var bounds = gameArea.rect;
        
        monster.transform.localPosition = new Vector2(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            bounds.min.y + 20f
        );
        
        return monster;
    }

    private void RegisterMonster(MonsterController monsterController)
    {
        savedMonIDs.Add(monsterController.monsterID);
        SaveSystem.SaveMonIDs(savedMonIDs);
        RegisterToActiveMons(monsterController);
    }

    public void RegisterToActiveMons(MonsterController monster)
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

    private void UpdateFoodPlacement()
    {
        var indicatorPos = ScreenToCanvasPosition(mainCanvas);
        foodPlacementIndicator.GetComponent<RectTransform>().anchoredPosition = indicatorPos;

        bool isValid = gameArea.rect.Contains(indicatorPos);
        foodPlacementIndicator.GetComponent<Image>().color = isValid ? validPositionColor : invalidPositionColor;
    }

    private void HandleFoodPlacementInput()
    {
        if (Input.GetMouseButtonDown(0)) TryPlaceFood();
        else if (Input.GetMouseButtonDown(1)) CancelPlacement();
    }

    private void TryPlaceFood()
    {
        var position = ScreenToCanvasPosition(mainCanvas);
        
        if (gameArea.rect.Contains(position))
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

    public void SpawnFood()
    {
        var food = GetPooledObject(_foodPool, foodPrefab);
        SetupPooledObject(food, gameArea, GetRandomPositionInGameArea());
    }
    #endregion

    #region Object Spawning
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

    public Vector2 GetRandomPositionInGameArea()
    {
        var bounds = gameArea.rect;
        return new Vector2(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            UnityEngine.Random.Range(bounds.min.y + 5, bounds.max.y - 5)
        );
    }

    public bool IsPositionInGameArea(Vector2 position) => gameArea.rect.Contains(position);

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
            Gizmos.DrawWireCube(gameArea.position, 
                new Vector3(gameArea.rect.width, gameArea.rect.height, 0));
        }
    }
    #endregion
}