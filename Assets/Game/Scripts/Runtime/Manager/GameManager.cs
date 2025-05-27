using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();
            LoadGame();

        }
        else Destroy(gameObject);
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
        UIManager.Instance.UpdateCoinCounter();
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
}