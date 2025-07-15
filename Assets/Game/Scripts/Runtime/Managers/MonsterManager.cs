using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class MonsterManager : MonoBehaviour
{
    #region Fields and Variables
    [Header("Prefabs & Pool Settings")]
    public GameObject monsterPrefab;
    public GameObject foodPrefab;
    public GameObject medicinePrefab;
    public GameObject poopPrefab;
    public GameObject coinPrefab;
    public RectTransform poolContainer;
    public int initialPoolSize = 50;
    public int initialMonsterPoolSize = 20;

    [Header("Game Settings")]
    public RectTransform gameAreaRT;
    public RectTransform groundRT; 
    public MonsterDatabaseSO monsterDatabase;
    public MonsterDatabaseSO npcMonsterDatabase;

    [Header("Food Placement Settings")]
    public GameObject foodPlacementIndicator;

    [Header("Rendering Settings")]
    public bool enableDepthSorting = true;
    private float lastSortTime = 0f;
    private float sortInterval = 0.1f;

    private Queue<GameObject> _foodPool = new Queue<GameObject>();
    private Queue<GameObject> _medicinePool = new Queue<GameObject>();
    private Queue<GameObject> _poopPool = new Queue<GameObject>();
    private Queue<GameObject> _coinPool = new Queue<GameObject>();
    private Queue<GameObject> _monsterPool = new Queue<GameObject>();

    public int poopCollected;
    public int coinCollected;
    public int maxMonstersSlots = 5;
    public int maxNPCSlots = 2;
    public int currentGameAreaIndex = 0;

    public List<MonsterController> activeMonsters = new List<MonsterController>();
    public List<MonsterController> npcMonsters = new List<MonsterController>();
    public List<CoinController> activeCoins = new List<CoinController>();
    public List<PoopController> activePoops = new List<PoopController>();
    public List<FoodController> activeFoods = new List<FoodController>();
    public List<MedicineController> activeMedicines = new List<MedicineController>();
    private List<string> savedMonIDs = new List<string>();

    public System.Action<int> OnCoinChanged;
    public System.Action<int> OnPoopChanged;
    #endregion

    #region Initialization and Setup
    private void Awake()
    {
        ServiceLocator.Register(this);
        InitializePools();
        SaveSystem.Initialize();
    }

    private void InitializePools()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePoolObject(foodPrefab, _foodPool);
            CreatePoolObject(poopPrefab, _poopPool);
            CreatePoolObject(coinPrefab, _coinPool);
        }
        
        // Initialize monster pool
        for (int i = 0; i < initialMonsterPoolSize; i++)
        {
            CreatePoolObject(monsterPrefab, _monsterPool);
        }
    }

    private void CreatePoolObject(GameObject prefab, Queue<GameObject> pool)
    {
        var obj = Instantiate(prefab, poolContainer);
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
    #endregion

    #region Game Loop
    void Start() => LoadGame();

    void Update()
    {
        if (enableDepthSorting && Time.time - lastSortTime >= sortInterval)
        {
            SortMonstersByDepth();
            lastSortTime = Time.time;
        }
    }
    #endregion

    #region Monster Management
    public void SellMonster(MonsterDataSO monsterData)
    {
        int sellPrice = monsterData.GetSellPrice(activeMonsters.Find(m => m.MonsterData.id == monsterData.id)?.evolutionLevel ?? 69);
        coinCollected += sellPrice;
        SaveSystem.SaveCoin(coinCollected);
        OnCoinChanged?.Invoke(coinCollected);
    }

    public void BuyMonster(MonsterDataSO monsterData)
    {
        int cost = monsterData.monsterPrice;
        if (SpentCoin(cost)) SpawnMonster(monsterData);
    }

    public void SpawnMonster(MonsterDataSO monsterData = null, string id = null)
    {
        GameObject monster = CreateMonster(monsterData);
        var controller = monster.GetComponent<MonsterController>();

        if (monster == null || controller == null)
        {
            Debug.LogError("SpawnMonster: Failed to create monster or MonsterController is missing.");
            return;
        }

        // Initialize ID 
        if (!string.IsNullOrEmpty(id))
        {
            controller.monsterID = id;
            var (_, evolutionLevel) = GetMonsterDataAndLevelFromID(id);
            if (evolutionLevel > 0) controller.evolutionLevel = evolutionLevel;
        }
        else
        {
            var data = controller.MonsterData;
            controller.monsterID = $"{data.id}_Stage{controller.evolutionLevel}_{System.Guid.NewGuid().ToString("N")[..8]}";
        }

        controller.LoadMonData();
        var finalData = controller.MonsterData;
        monster.name = $"{finalData.monsterName}_{controller.monsterID}";

        if (string.IsNullOrEmpty(id))
        {
            RegisterNewMonster(controller);
        }
        else
            RegisterLoadedMonster(controller);

        // Apply current pet scale to newly spawned monster
        var settingsManager = ServiceLocator.Get<SettingsManager>();
        if (settingsManager != null)
        {
            settingsManager.ApplyCurrentPetScaleToMonster(controller);
        }
    }

    private GameObject CreateMonster(MonsterDataSO monsterData)
    {
        var monster = GetPooledObject(_monsterPool, monsterPrefab);
        monster.transform.SetParent(gameAreaRT, false);
        
        var monsterController = monster.GetComponent<MonsterController>();
        var rectTransform = monster.GetComponent<RectTransform>();
        var movementBounds = new MonsterBoundsHandler(this, rectTransform);
        Vector2 spawnPosition = movementBounds.GetRandomSpawnTarget();
        monster.transform.localPosition = spawnPosition;

        if (monsterController != null)
        {
            MonsterDataSO _data = monsterData;
            if (_data == null)
            {
                Debug.LogError("SpawnMonster: No monster data provided");
                return null;
            }
            else
                monsterController.SetMonsterData(_data);
        }
        else
        {
            Debug.LogError("SpawnMonster: MonsterController component not found on the prefab");
            return null;
        }
        
        monster.SetActive(true);
        return monster;
    }

    private void RegisterNewMonster(MonsterController monsterController)
    {
        savedMonIDs.Add(monsterController.monsterID);
        SaveSystem.SaveMonIDs(savedMonIDs);
        RegisterLoadedMonster(monsterController);
    }

    private void RegisterLoadedMonster(MonsterController monster)
    {
        if (!activeMonsters.Contains(monster))
        {
            activeMonsters.Add(monster);
        }
    }

    public void RemoveSavedMonsterID(string monsterID)
    {
        activeMonsters.RemoveAll(m => m.monsterID == monsterID);
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

    private (MonsterDataSO, int) GetMonsterDataAndLevelFromID(string monsterID)
    {
        var parts = monsterID.Split('_');
        if (parts.Length >= 2)
        {
            string monsterTypeId = parts[0];
            string levelPart = parts[1];
            int evolutionLevel = 0;

            if (levelPart.StartsWith("Stage"))
            {
                if (!int.TryParse(levelPart.Substring(5), out evolutionLevel))
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

        // Update sibling indices, starting from index 1 to preserve background at index 0
        for (int i = 0; i < validMonsters.Count; i++)
        {
            validMonsters[i].transform.SetSiblingIndex(i + 1); // +1 to skip background
        }
    }
    #endregion

    #region Consumable Management
    public void SpawnItem(ItemDataSO data, Vector2 pos)
    {
        GameObject pooled = null;

        switch (data.category)
        {
            case ItemType.Food:
                pooled = GetPooledObject(_foodPool, foodPrefab);
                break;

            case ItemType.Medicine:
                pooled = GetPooledObject(_medicinePool, medicinePrefab);
                break;

            default:
                Debug.LogWarning($"SpawnItem: Unsupported item type: {data.category}");
                return;
        }

        if (pooled != null)
        {
            SetupPooledObject(pooled, gameAreaRT, pos);

            if (pooled.TryGetComponent<IConsumable>(out var consumable))
            {
                consumable.Initialize(data, groundRT);
            }

            // Register into the correct active list
            switch (data.category)
            {
                case ItemType.Food:
                    if (pooled.TryGetComponent<FoodController>(out var foodCtrl) && !activeFoods.Contains(foodCtrl))
                    {
                        activeFoods.Add(foodCtrl);
                    }
                    break;

                case ItemType.Medicine:
                    if (pooled.TryGetComponent<MedicineController>(out var medCtrl) && !activeMedicines.Contains(medCtrl))
                    {
                        activeMedicines.Add(medCtrl);
                    }
                    break;
            }
        }
    }
    #endregion

    #region Item Management
    public GameObject SpawnCoinWithArc(Vector2 startPosition, Vector2 targetPosition, CoinType type)
    {
        var coin = GetPooledObject(_coinPool, coinPrefab);
        if (coin != null)
        {
            SetupPooledObject(coin, gameAreaRT, startPosition);
            activeCoins.Add(coin.GetComponent<CoinController>());
            coin.GetComponent<CoinController>().Initialize(type);

            // Start arc animation coroutine
            StartCoroutine(AnimateCoinArc(coin.transform, startPosition, targetPosition));
        }
        return coin;
    }

    private IEnumerator AnimateCoinArc(Transform coinTransform, Vector2 startPos, Vector2 endPos)
    {
        float duration = 0.8f; // Arc animation duration
        float arcHeight = 100f; // How high the coin goes
        float elapsedTime = 0f;

        RectTransform coinRect = coinTransform.GetComponent<RectTransform>();
        if (coinRect == null) yield break;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            // Ease out curve for natural falling motion
            float easedT = 1f - (1f - t) * (1f - t);

            // Calculate arc position
            Vector2 currentPos = Vector2.Lerp(startPos, endPos, easedT);

            // Add vertical arc (parabolic motion)
            float arcProgress = 4f * t * (1f - t); // Peaks at t=0.5
            currentPos.y += arcHeight * arcProgress;

            coinRect.anchoredPosition = currentPos;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final position
        coinRect.anchoredPosition = endPos;

        // Optional: Add a small bounce effect on landing
        StartCoroutine(CoinBounceEffect(coinRect));
    }

    private IEnumerator CoinBounceEffect(RectTransform coinRect)
    {
        Vector2 finalPos = coinRect.anchoredPosition;
        float bounceHeight = 15f;
        float bounceDuration = 0.3f;
        float elapsedTime = 0f;

        while (elapsedTime < bounceDuration)
        {
            float t = elapsedTime / bounceDuration;
            float bounce = bounceHeight * (1f - t) * Mathf.Sin(t * Mathf.PI * 3f); // 3 small bounces

            coinRect.anchoredPosition = finalPos + Vector2.up * bounce;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        coinRect.anchoredPosition = finalPos;
    }

    public GameObject SpawnPoopAt(Vector2 anchoredPos, PoopType type)
    {
        // Find a non-overlapping position
        Vector2 finalPos = FindNonOverlappingPosition(anchoredPos, 50f);

        var poop = GetPooledObject(_poopPool, poopPrefab);
        SetupPooledObject(poop, gameAreaRT, finalPos);
        poop.GetComponent<PoopController>().Initialize(type);
        return poop;
    }

    private Vector2 FindNonOverlappingPosition(Vector2 preferredPos, float minDistance)
    {
        Vector2 testPos = preferredPos;
        int maxAttempts = 10;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            bool hasOverlap = false;

            // Check against active coins and poop
            foreach (Transform child in gameAreaRT)
            {
                if (child.gameObject.activeInHierarchy &&
                    (child.GetComponent<CoinController>() != null || child.GetComponent<PoopController>() != null))
                {
                    float distance = Vector2.Distance(testPos, child.GetComponent<RectTransform>().anchoredPosition);
                    if (distance < minDistance)
                    {
                        hasOverlap = true;
                        break;
                    }
                }
            }

            if (!hasOverlap)
                return testPos;

            // Try a random nearby position
            Vector2 randomOffset = Random.insideUnitCircle * (minDistance * 2f);
            testPos = preferredPos + randomOffset;

            // Keep within game area bounds
            var rect = gameAreaRT.rect;
            testPos.x = Mathf.Clamp(testPos.x, rect.xMin + 25f, rect.xMax - 25f);
            testPos.y = Mathf.Clamp(testPos.y, rect.yMin + 25f, rect.yMax - 25f);
        }

        return testPos;
    }

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
    #endregion

    #region Object Pooling
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
        else if (obj.TryGetComponent<MedicineController>(out var medicine))
        {
            _medicinePool.Enqueue(obj);
            activeMedicines.Remove(medicine);
        }
        else if (obj.TryGetComponent<CoinController>(out _))
        {
            _coinPool.Enqueue(obj);
            activeCoins.Remove(obj.GetComponent<CoinController>());
        }
        else if (obj.TryGetComponent<MonsterController>(out var monsterController))
        {
            _monsterPool.Enqueue(obj);
            activeMonsters.Remove(monsterController);
            npcMonsters.Remove(monsterController);
        }
        else if (obj.name.Contains("Poop"))
        {
            _poopPool.Enqueue(obj);
            CollectPoop();
            activePoops.Remove(obj.GetComponent<PoopController>());
        }
    }
    #endregion

    #region Save and Load
    private void LoadGame()
    {
        coinCollected = SaveSystem.LoadCoin();
        poopCollected = SaveSystem.LoadPoop();
        
        // Load the last active game area
        currentGameAreaIndex = SaveSystem.LoadActiveGameAreaIndex();
        
        // Load all saved monster IDs but only spawn monsters for current area
        savedMonIDs = SaveSystem.LoadMonIDs();
        LoadMonstersForCurrentArea();
        
        OnCoinChanged?.Invoke(coinCollected);
        OnPoopChanged?.Invoke(poopCollected);
    }

    private void LoadMonstersForCurrentArea()
    {
        foreach (var id in savedMonIDs)
        {
            if (SaveSystem.LoadMon(id, out MonsterSaveData saveData))
            {
                // Only spawn monsters that belong to the current game area
                if (saveData.gameAreaId == currentGameAreaIndex)
                {
                    var (monsterData, evolutionLevel) = GetMonsterDataAndLevelFromID(id);
                    if (monsterData != null)
                    {
                        SpawnMonster(monsterData, id);
                    }
                }
            }
        }
    }

    public void SaveAllMons()
    {
        foreach (var monster in activeMonsters)
        {
            var saveData = new MonsterSaveData
            {
                instanceId = monster.monsterID,
                monsterId = monster.MonsterData.id,
                gameAreaId = currentGameAreaIndex, // Save current area
                currentHunger = monster.StatsHandler.CurrentHunger,
                currentHappiness = monster.StatsHandler.CurrentHappiness,
                currentHealth = monster.StatsHandler.CurrentHP,
                currentEvolutionLevel = monster.evolutionLevel,
                
                // Evolution data
                timeCreated = monster.GetEvolveTimeCreated(),
                totalTimeSinceCreation = monster.GetEvolveTimeSinceCreation(),
                nutritionConsumed = monster.GetEvolveNutritionConsumed(),
                currentInteraction = monster.GetEvolutionInteractionCount()
            };
            SaveSystem.SaveMon(saveData);
        }
        SaveSystem.SaveMonIDs(savedMonIDs);
        SaveSystem.SaveActiveGameAreaIndex(currentGameAreaIndex); // Save current area
    }

    // Add method to switch game areas
    public void SwitchToGameArea(int areaIndex)
    {
        if (areaIndex == currentGameAreaIndex) return; // Already in this area
        
        // Save current monsters before switching
        SaveAllMons();
        
        // Clear current active monsters (return to pool)
        var monstersToRemove = activeMonsters.ToList();
        foreach (var monster in monstersToRemove)
        {
            DespawnToPool(monster.gameObject);
        }
        activeMonsters.Clear();
        
        // Clear other active objects too
        ClearActiveObjects();
        
        // Update current area
        currentGameAreaIndex = areaIndex;
        SaveSystem.SaveActiveGameAreaIndex(currentGameAreaIndex);
        
        // Load monsters for the new area
        LoadMonstersForCurrentArea();
        
        Debug.Log($"Switched to game area {areaIndex}");
    }

    private void ClearActiveObjects()
    {
        // Clear coins
        var coinsToRemove = activeCoins.ToList();
        foreach (var coin in coinsToRemove)
        {
            if (coin != null && coin.gameObject != null)
                DespawnToPool(coin.gameObject);
        }
        activeCoins.Clear();
        
        // Clear poop
        var poopsToRemove = activePoops.ToList();
        foreach (var poop in poopsToRemove)
        {
            if (poop != null && poop.gameObject != null)
                DespawnToPool(poop.gameObject);
        }
        activePoops.Clear();
        
        // Clear food
        var foodsToRemove = activeFoods.ToList();
        foreach (var food in foodsToRemove)
        {
            if (food != null && food.gameObject != null)
                DespawnToPool(food.gameObject);
        }
        activeFoods.Clear();
        
        // Clear medicine
        var medicinestoRemove = activeMedicines.ToList();
        foreach (var medicine in medicinestoRemove)
        {
            if (medicine != null && medicine.gameObject != null)
                DespawnToPool(medicine.gameObject);
        }
        activeMedicines.Clear();
    }

    // Helper method to get monster count for specific area
    public int GetMonsterCountForArea(int areaIndex)
    {
        return SaveSystem.PlayerConfig.GetMonsterCountForGameArea(areaIndex);
    }

    // Helper method to move monster to different area
    public void MoveMonsterToArea(string monsterID, int targetAreaIndex)
    {
        var monster = activeMonsters.Find(m => m.monsterID == monsterID);
        if (monster != null)
        {
            // Update the monster's area in save data
            SaveSystem.PlayerConfig.SetMonsterGameArea(monsterID, targetAreaIndex);
            
            // If moving to different area than current, remove from active list
            if (targetAreaIndex != currentGameAreaIndex)
            {
                DespawnToPool(monster.gameObject);
            }
            
            SaveSystem.SaveAll();
        }
    }

    public MonsterController GetMonsterByID(string monsterID)
    {
        if (SaveSystem.LoadMon(monsterID, out var saveData))
        {
            MonsterController monster = new MonsterController();
            monster.StatsHandler.Initialize(saveData.currentHunger, saveData.currentHappiness, saveData.currentHealth, monster.MonsterData.GetMaxHealth(saveData.currentEvolutionLevel));
            return monster;
        }
        return null;
    }

    private void SaveGameData()
    {
        SaveAllMons();
        SaveSystem.SavePoop(poopCollected);
        SaveSystem.SaveCoin(coinCollected);
        SaveSystem.Flush();
    }
    #endregion

    #region Utility Methods
    public bool IsPositionInGameArea(Vector2 localPosition)
    {
        var rect = gameAreaRT.rect;
        return localPosition.x >= rect.xMin && localPosition.x <= rect.xMax &&
               localPosition.y >= rect.yMin && localPosition.y <= rect.yMax;
    }

    public bool IsPositionClearOfObjects(Vector2 position, float radius = 40f)
    {
        // Check coins
        foreach (var coin in activeCoins)
        {
            if (coin != null && coin.gameObject.activeInHierarchy)
            {
                var coinRect = coin.GetComponent<RectTransform>();
                if (coinRect != null)
                {
                    float distance = Vector2.Distance(position, coinRect.anchoredPosition);
                    if (distance < radius) return false;
                }
            }
        }

        // Check poop
        foreach (var poop in activePoops)
        {
            if (poop != null && poop.gameObject.activeInHierarchy)
            {
                var poopRect = poop.GetComponent<RectTransform>();
                if (poopRect != null)
                {
                    float distance = Vector2.Distance(position, poopRect.anchoredPosition);
                    if (distance < radius) return false;
                }
            }
        }

        return true;
    }
    #endregion

    #region Lifecycle Events
    void OnApplicationQuit() => SaveGameData();
    void OnApplicationPause(bool pauseStatus) { if (pauseStatus) SaveGameData(); }

#if UNITY_EDITOR
    void OnDisable() { if (Application.isPlaying) SaveGameData(); }
#endif

    void OnDestroy() => ServiceLocator.Unregister<MonsterManager>();
    #endregion

    #region NPC Monster Management
    public void SpawnNPCMonster(MonsterDataSO monsterData)
    {
        if (monsterData == null)
        {
            Debug.LogError("No monster data provided and no database available.");
            return;
        }

        // Use pooled monster object
        GameObject npcObj = GetPooledObject(_monsterPool, monsterPrefab);
        npcObj.transform.SetParent(gameAreaRT, false);
        
        var controller = npcObj.GetComponent<MonsterController>();
        var movementBounds = new MonsterBoundsHandler(this, npcObj.GetComponent<RectTransform>());
        
        // Flag it as an NPC
        controller.isNPC = true;
        
        // Generate a unique ID for the NPC
        string npcID = $"NPC_{monsterData.id}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
        controller.monsterID = npcID;
        controller.gameObject.name = $"{monsterData.monsterName}_{npcID}";
        
        // Set up the monster with data
        controller.SetMonsterData(monsterData);
        
        // Position the NPC
        npcObj.GetComponent<RectTransform>().anchoredPosition = movementBounds.GetRandomSpawnTarget();
        npcObj.SetActive(true);

        // Track the NPC
        npcMonsters.Add(controller);

        // Apply current pet scale
        var settingsManager = ServiceLocator.Get<SettingsManager>();
        if (settingsManager != null)
        {
            settingsManager.ApplyCurrentPetScaleToMonster(controller);
        }
    }
    #endregion
}