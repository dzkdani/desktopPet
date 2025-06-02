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
    private float sortInterval = 0.1f; // Sort every 0.1 seconds to avoid performance issues

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
                lastHappiness = monster.currentHappiness,
                isEvolved = monster.isEvolved,
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

    private void SpawnMonster(MonsterDataSO monsterData = null, string existingID = null)
    {
        GameObject monster;
        
        if (monsterData != null)
            monster = CreateMonsterByData(monsterData);
        else
            monster = CreateMonster();
        
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

    public void SpawnMonsterFromGacha(MonsterDataSO monsterData) 
    {
        SpawnMonster(monsterData);
    }

    private void SpawnLoadedMons(string monID) 
    {
        var (monsterData, _) = GetMonsterDataAndLevelFromID(monID);
        SpawnMonster(monsterData, monID);
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
                int.TryParse(levelPart.Substring(2), out evolutionLevel);
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
            monsterController.SetMonsterData(randomMonsterData);
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
            monsterController.SetMonsterData(monsterData);
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

    public void DespawnPools(GameObject obj)
    {
        obj.transform.SetParent(poolContainer, false);
        obj.SetActive(false);

        if (obj.name.Contains("Poop")) _poopPool.Enqueue(obj);
        else if (obj.name.Contains("Coin")) _coinPool.Enqueue(obj);
        else if (obj.name.Contains("Food")) _foodPool.Enqueue(obj);
    }

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

        // Create a list of monsters with their Y positions for sorting
        var monstersWithY = new List<(MonsterController monster, float yPos)>();
        
        foreach (var monster in activeMonsters)
        {
            if (monster != null && monster.gameObject.activeInHierarchy)
            {
                var rectTransform = monster.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    monstersWithY.Add((monster, rectTransform.anchoredPosition.y));
                }
            }
        }

        // Sort by Y position (higher Y should be behind = lower sibling index)
        monstersWithY.Sort((a, b) => b.yPos.CompareTo(a.yPos));

        // Update sibling indices
        for (int i = 0; i < monstersWithY.Count; i++)
        {
            var monster = monstersWithY[i].monster;
            if (monster != null)
            {
                monster.transform.SetSiblingIndex(i);
            }
        }
    }
}