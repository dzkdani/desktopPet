using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

    [Header("Prefabs & Pool Settings")]
    public GameObject monPrefab;
    public GameObject foodPrefab;
    public GameObject poopPrefab;
    public GameObject coinPrefab;
    public RectTransform poolContainer;
    [Tooltip("Initial pool size for each type")]
    public int initialPoolSize = 20;
    private Queue<GameObject> _foodPool = new Queue<GameObject>();
    private Queue<GameObject> _poopPool = new Queue<GameObject>();
    private Queue<GameObject> _coinPool = new Queue<GameObject>();
    [Header("Game Settings")]
    public RectTransform gameArea;
    [HideInInspector] public int poopCollected;
    [HideInInspector] public int coinCollected;
    public Canvas mainCanvas;
    [HideInInspector] public List<MonsterController> activeMonsters = new List<MonsterController>();
    public List<FoodController> activeFoods = new List<FoodController>();
    private List<string> savedMonIDs = new List<string>();
    public MonsterDatabaseSO monsterDatabase;
    private float groundPositionY = -33f;
    [Header("Food Placement Settings")]
    public GameObject foodPlacementIndicator; // Assign a semi-transparent food sprite in inspector
    public Color validPositionColor = Color.green;
    public Color invalidPositionColor = Color.red;
    private bool isInPlacementMode = false;
    private int pendingFoodCost = 0;

    private void Awake()
    {
        ServiceLocator.Register(this);
        InitializePools();
    }
    void Start()
    {
        LoadGame();

    }
    private void InitializePools()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            var f = Instantiate(foodPrefab, poolContainer);
            f.SetActive(false);
            _foodPool.Enqueue(f);

            var p = Instantiate(poopPrefab, poolContainer);
            p.SetActive(false);
            _poopPool.Enqueue(p);

            var c = Instantiate(coinPrefab, poolContainer);
            c.SetActive(false);
            _coinPool.Enqueue(c);
        }
    }
    void Update()
    {
        if (isInPlacementMode)
        {
            UpdateFoodPlacement();

            if (Input.GetMouseButtonDown(0)) // Left click confirms
            {
                TryPlaceFood();
            }
            else if (Input.GetMouseButtonDown(1)) // Right click cancels
            {
                CancelPlacement();
            }
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

    private void UpdateFoodPlacement()
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gameArea,
            Input.mousePosition,
            mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCanvas.worldCamera,
            out localPos
        );

        // Move the indicator
        foodPlacementIndicator.GetComponent<RectTransform>().anchoredPosition = ScreenToCanvasPosition(mainCanvas);

        // Check if position is valid
        bool isValid = gameArea.rect.Contains(localPos);
        foodPlacementIndicator.GetComponent<Image>().color = isValid ? validPositionColor : invalidPositionColor;
    }


    private void TryPlaceFood()
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gameArea,
            Input.mousePosition,
            mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCanvas.worldCamera,
            out localPos
        );

        if (gameArea.rect.Contains(localPos))
        {
            if (SpentCoin(pendingFoodCost))
            {
                SpawnFoodAtPosition(localPos);
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
        GameObject f = _foodPool.Count > 0 ? _foodPool.Dequeue()
                                         : Instantiate(foodPrefab, poolContainer);
        var rt = f.GetComponent<RectTransform>();
        rt.SetParent(gameArea, false);
        rt.anchoredPosition = position;
        f.SetActive(true);

        var foodController = f.GetComponent<FoodController>();
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
        SpawnFoodAt();
    }

    public void SpawnFoodAt()
    {
        GameObject f = _foodPool.Count > 0 ? _foodPool.Dequeue()
                                           : Instantiate(foodPrefab, poolContainer);
        var rt = f.GetComponent<RectTransform>();
        rt.SetParent(gameArea, false);
        rt.anchoredPosition = GetRandomPositionInGameArea();
        f.SetActive(true);
    }
    public GameObject SpawnCoinAt(Vector2 anchoredPos, CoinType type)
    {
        GameObject c = _coinPool.Count > 0 ? _coinPool.Dequeue()
                                           : Instantiate(coinPrefab, poolContainer);
        var rt = c.GetComponent<RectTransform>();
        rt.SetParent(gameArea, false);
        rt.anchoredPosition = anchoredPos;
        c.SetActive(true);

        CoinController coinController = c.GetComponent<CoinController>();
        coinController.Initialize(type);

        return c;
    }

    public GameObject SpawnPoopAt(Vector2 anchoredPos)
    {
        GameObject p = _poopPool.Count > 0 ? _poopPool.Dequeue()
                                           : Instantiate(poopPrefab, poolContainer);
        var rt = p.GetComponent<RectTransform>();
        rt.SetParent(gameArea, false);
        rt.anchoredPosition = anchoredPos;
        p.SetActive(true);
        return p;
    }

    public void DespawnFood(GameObject food)
    {
        food.transform.SetParent(poolContainer, false);
        food.SetActive(false);
        _foodPool.Enqueue(food);
    }

    public void DespawnPoop(GameObject poop)
    {
        poop.transform.SetParent(poolContainer, false);
        poop.SetActive(false);
        _poopPool.Enqueue(poop);
    }

    public void DespawnCoin(GameObject coin)
    {
        coin.transform.SetParent(poolContainer, false);
        coin.SetActive(false);
        _coinPool.Enqueue(coin);
    }

    public void BuyMons(int cost = 10)
    {
        if (SpentCoin(cost))
            SpawnNewMon();
        else
            Debug.Log("Not enough coins to buy a mons!");
    }

    public void RegisterToActiveMons(MonsterController monster)
    {
        if (!activeMonsters.Contains(monster))
        {
            activeMonsters.Add(monster);
        }
    }

    public bool SpentCoin(int amount)
    {
        if (coinCollected < amount) return false;
        coinCollected -= amount;
        SaveSystem.SaveCoin(coinCollected);
        ServiceLocator.Get<UIManager>().UpdateCoinCounter();
        return true;
    }


    private void SpawnLoadedMons(string monID)
    {
        GameObject monster = Instantiate(monPrefab, gameArea);
        Vector2 min = gameArea.rect.min;
        Vector2 max = gameArea.rect.max;
        monster.transform.localPosition = new Vector2(
            UnityEngine.Random.Range(min.x, max.x), groundPositionY
        );
        MonsterController monsterController = monster.GetComponent<MonsterController>();
        monsterController.monsterID = monID;
        monsterController.LoadMonData();
        monster.SetActive(true);
    }
    private void SpawnNewMon()
    {
        GameObject monster = Instantiate(monPrefab, gameArea);
        Vector2 min = gameArea.rect.min;
        Vector2 max = gameArea.rect.max;
        monster.transform.localPosition = new Vector2(
            UnityEngine.Random.Range(min.x, max.x), groundPositionY
        );
        MonsterController monsterController = monster.GetComponent<MonsterController>();
        monsterController.monsterID = GenerateRandomID(8);
        savedMonIDs.Add(monsterController.monsterID);
        PlayerPrefs.SetString("SavedMonIDs", string.Join(",", savedMonIDs));
    }
    public void SpawnLoadedMonsViaGacha(string monID)
    {
        GameObject monster = Instantiate(monPrefab, gameArea);
        Vector2 min = gameArea.rect.min;
        Vector2 max = gameArea.rect.max;
        monster.transform.localPosition = new Vector2(
            UnityEngine.Random.Range(min.x, max.x), -33f
        );
        MonsterController monsterController = monster.GetComponent<MonsterController>();
        monsterController.monsterID = monID;
        monsterController.LoadMonData();
        savedMonIDs.Add(monID);
        activeMonsters.Add(monsterController);
        PlayerPrefs.SetString("SavedMonIDs", string.Join(",", savedMonIDs));
        monster.SetActive(true);
    }


    private string GenerateRandomID(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        char[] id = new char[length];
        for (int i = 0; i < length; i++)
        {
            id[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
        }
        return new string(id);
    }

    public Vector2 GetRandomPositionInGameArea()
    {
        Vector2 min = gameArea.rect.min;
        Vector2 max = gameArea.rect.max;
        return new Vector2(
            UnityEngine.Random.Range(min.x, max.x),
            UnityEngine.Random.Range(min.y + 5, max.y - 5)
        );
    }


    public bool IsPositionInGameArea(Vector2 position)
    {
        return gameArea.rect.Contains(position);
    }
    void OnDrawGizmosSelected()
    {
        if (gameArea != null)
        {
            Gizmos.color = Color.green;
            Vector3 center = gameArea.position;
            Vector3 size = new Vector3(gameArea.rect.width, gameArea.rect.height, 0);
            Gizmos.DrawWireCube(center, size);
        }
    }


    public static Vector2 ScreenToCanvasPosition(Canvas canvas)
    {
        // 1. Get the RectTransform of the Canvas
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        // 2. Convert screen point to local point
        Vector2 localPoint;
        // For Screen Space - Overlay canvases, pass `null` as the camera.
        // For Screen Space - Camera or World Space canvases, pass canvas.worldCamera.
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            cam,
            out localPoint
        );

        return localPoint;
    }

    private void LoadGame()
    {
        coinCollected = SaveSystem.LoadCoin();
        savedMonIDs = SaveSystem.LoadSavedMonIDs();
        Debug.Log($"Loaded {savedMonIDs.Count} saved monss with IDs: {string.Join(", ", savedMonIDs)}");
        foreach (var id in savedMonIDs)
            if (SaveSystem.LoadMon(id, out var d))
                SpawnLoadedMons(id);
    }
    public void SaveAllMons()
    {
        foreach (var monster in activeMonsters)
            SaveSystem.SaveMon(new MonsterSaveData
            {
                monsterId = monster.monsterID,
                lastHunger = monster.currentHunger,
                isEvolved = monster.isEvolved,
            });
        SaveSystem.SaveMonIDs(savedMonIDs);
        SaveSystem.Flush();
    }

    void OnApplicationQuit()
    {
        SaveAllMons();
        SaveSystem.SavePoop(poopCollected);
        SaveSystem.SaveCoin(coinCollected);
        SaveSystem.Flush();
    }
    void OnDestroy()
    {
        ServiceLocator.Unregister<GameManager>();
    }
}