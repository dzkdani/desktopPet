using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Prefabs & Pool Settings")]
    public GameObject foodPrefab;
    public GameObject poopPrefab;
    public GameObject petPrefab;
    [Tooltip("Initial pool size for each type")]
    public int initialPoolSize = 20;
    private Queue<GameObject> _foodPool = new Queue<GameObject>();
    private Queue<GameObject> _poopPool = new Queue<GameObject>();

    [Header("Game Area")]
    public RectTransform gameArea;
    [Header("Game Settings")]
    public int petCost = 10;
    public int MaxPets = 5;
    [Header("Pet Variety")]
    public string[] possiblePetNames;
    [HideInInspector] public int poopCollected = 100;

    [HideInInspector] public List<PetController> activePets = new List<PetController>();
    public List<FoodController> activeFoods = new List<FoodController>();
    private List<string> savedPetIDs = new List<string>();

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
            var f = Instantiate(foodPrefab, gameArea);
            f.SetActive(false);
            _foodPool.Enqueue(f);

            var p = Instantiate(poopPrefab, gameArea);
            p.SetActive(false);
            _poopPool.Enqueue(p);
        }
    }

    public GameObject SpawnFoodAt(Vector2 anchoredPos)
    {
        GameObject f = _foodPool.Count > 0 ? _foodPool.Dequeue() 
                                           : Instantiate(foodPrefab, gameArea);
        var rt = f.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        f.SetActive(true);
        return f;
    }

    public GameObject SpawnPoopAt(Vector2 anchoredPos)
    {
        GameObject p = _poopPool.Count > 0 ? _poopPool.Dequeue() 
                                           : Instantiate(poopPrefab, gameArea);
        var rt = p.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        p.SetActive(true);
        return p;
    }

    public void DespawnFood(GameObject food)
    {
        food.SetActive(false);
        _foodPool.Enqueue(food);
    }

    public void DespawnPoop(GameObject poop)
    {
        poop.SetActive(false);
        _poopPool.Enqueue(poop);
    }

    public void RegisterPet(PetController pet)
    {
        if (!activePets.Contains(pet))
        {
            activePets.Add(pet);
        }
    }

    private void LoadGame()
    {
        poopCollected = SaveSystem.LoadMoney();
        savedPetIDs   = SaveSystem.LoadSavedPetIDs();
        foreach (var id in savedPetIDs)
            if (SaveSystem.TryLoadPet(id, out var d))
                SpawnSavedPets(id);
    }

    public bool SpendMoney(int amount)
    {
        if (poopCollected < amount) return false;
        poopCollected -= amount;
        SaveSystem.SaveMoney(poopCollected);
        UIManager.Instance.UpdatePoopCounter();
        return true;
    }

    public void SaveAllPets()
    {
        foreach (var pet in activePets)
            SaveSystem.SavePet(new PetData {
                petName  = pet.petID,
                hunger   = pet.currentHunger,
                position = pet.GetComponent<RectTransform>().anchoredPosition
            });
        SaveSystem.SavePetIDs(savedPetIDs);
        SaveSystem.Flush();
    }

    private void SpawnSavedPets(string petID)
    {
        GameObject pet = Instantiate(petPrefab, gameArea);
        PetController petController = pet.GetComponent<PetController>();
        petController.petID = petID;
        petController.LoadPetData();
        pet.SetActive(true);
    }
    private void SpawnNewPet()
    {
        GameObject pet = Instantiate(petPrefab, gameArea);
        PetController petController = pet.GetComponent<PetController>();
        petController.petID = GenerateRandomID(8);
        savedPetIDs.Add(petController.petID);
        PlayerPrefs.SetString("SavedPetIDs", string.Join(",", savedPetIDs));
    }
    public string GenerateRandomID(int length)
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
            UnityEngine.Random.Range(min.y, max.y)
        );
    }

    public void BuyPet()
    {
        if (SpendMoney(petCost))
        {
            SpawnNewPet();
        }
        else
        {
            Debug.Log("Not enough money to buy a pet");
        }
    }

    void DebugPetData()
    {
        string savedIds = PlayerPrefs.GetString("SavedPetIDs");
        Debug.Log("Saved Pet IDs: " + savedIds);
        foreach (string id in savedIds.Split(','))
        {
            string petData = PlayerPrefs.GetString($"Pet{id}");
            Debug.Log($"Pet ID: {id}, Data: {petData}");
        }
    }
    public void SpawnFood()
    {
        GameObject food = Instantiate(GameManager.Instance.foodPrefab,
            GameManager.Instance.gameArea);
        food.GetComponent<RectTransform>().anchoredPosition =
            GameManager.Instance.GetRandomPositionInGameArea();
    }

    public bool IsPositionInGameArea(Vector2 position)
    {
        return gameArea.rect.Contains(position);
    }
    // Call this when food is spawned
    public void RegisterFood(FoodController food)
    {
        activeFoods.Add(food);
    }

    // Call this when food is destroyed
    public void UnregisterFood(FoodController food)
    {
        activeFoods.Remove(food);
    }
    void OnApplicationQuit()
    {
        SaveAllPets();
        PlayerPrefs.SetInt("Money", poopCollected);
        DebugPetData();
        PlayerPrefs.Save();
    }
}