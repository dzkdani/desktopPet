using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using Newtonsoft.Json;


public static class SaveSystem
{
    private const string SaveFileName = "playerConfig.json";
    private static PlayerConfig _playerConfig;
    public static PlayerConfig PlayerConfig => _playerConfig;
    private static DateTime _sessionStartTime;

    public static void SaveCoin(int money) => _playerConfig.coins = money;
    public static int LoadCoin() => _playerConfig.coins;
    public static void SavePoop(int poop) => _playerConfig.poops = poop; // Directly save to PlayerConfig
    public static int LoadPoop() => _playerConfig.poops;

    public static void Initialize()
    {
        LoadPlayerConfig();
        _sessionStartTime = DateTime.Now;

        Debug.Log($"Last login time: {_playerConfig.lastLoginTime}");
        // Handle first-time login
        if (_playerConfig.lastLoginTime == default)
        {
            _playerConfig.lastLoginTime = DateTime.Now;
        }

        // Check for time cheating
        CheckTimeDiscrepancy();
    }
    
    // Save all data when application pauses/quits
    public static void SaveAll()
    {
        UpdatePlayTime();
        SavePlayerConfig();
    }

    public static void SaveMon(MonsterSaveData data)
    {
        if (data == null || string.IsNullOrEmpty(data.monsterId))
        {
            Debug.LogWarning("Tried to save null or invalid monster data.");
            return;
        }
        _playerConfig.SaveMonsterData(data);
        SaveAll(); // Make sure to save the whole config after updating
    }

    public static bool LoadMon(string monsterId, out MonsterSaveData data)
    {
        if (string.IsNullOrEmpty(monsterId))
        {
            data = null;
            return false;
        }

        return _playerConfig.LoadMonsterData(monsterId, out data);
    }

    public static void DeleteMon(string instanceID)
    {
        if (string.IsNullOrEmpty(instanceID)) return;

        _playerConfig.DeleteMonster(instanceID);
        SaveAll(); // Ensure the config is persisted
    }

    public static void SaveMonIDs(List<string> ids)
    {
        _playerConfig.SetAllMonsterIDs(ids);
        SavePlayerConfig();
    }

    public static List<string> LoadMonIDs()
    {
        return _playerConfig.GetAllMonsterIDs();
    }

    public static void Flush() => PlayerPrefs.Save();

    public static void ResetSaveData()
    {
        SaveCoin(100);
        SavePoop(0);
        _playerConfig.ClearAllMonsterData();
        _playerConfig.ClearAllNPCMonsterData();
        _playerConfig.ownedItems.Clear();
        _playerConfig.ownedBiomes.Clear();
        _playerConfig.activeBiomeID = "default_biome";
        SavePlayerConfig();
    }


    #region Time Tracking
    public static DateTime GetLastLoginTime()
    {
        return _playerConfig.lastLoginTime;
    }

    public static TimeSpan GetTotalPlayTime()
    {
        return _playerConfig.totalPlayTime;
    }

    public static TimeSpan GetCurrentSessionPlayTime()
    {
        return DateTime.Now - _sessionStartTime;
    }

    private static void UpdatePlayTime()
    {
        _playerConfig.totalPlayTime += GetCurrentSessionPlayTime();
        _playerConfig.lastLoginTime = DateTime.Now;
    }

    private static void CheckTimeDiscrepancy()
    {
        DateTime currentTime = DateTime.Now;
        TimeSpan timeSinceLastLogin = currentTime - _playerConfig.lastLoginTime;

        // Detect if system time was set backwards
        if (timeSinceLastLogin.TotalSeconds < 0)
        {
            Debug.LogWarning("System time was set backwards! Possible cheating attempt.");
            // Handle time cheating (e.g., penalize player or reset progress)
            HandleTimeCheating();
        }

        // Detect if player was away for too long
        if (timeSinceLastLogin.TotalHours > 24)
        {
            Debug.Log($"Player was away for {timeSinceLastLogin.TotalHours} hours");
            // Handle long absence (e.g., update monster states)
            // HandleLongAbsence(timeSinceLastLogin);
        }
    }

    private static void HandleTimeCheating()
    {
        // Implement your anti-cheat measures here
        // For example: reduce coins, reset progress, or show warning
        _playerConfig.coins = Mathf.Max(0, _playerConfig.coins - 100);
    }

    private static void HandleLongAbsence(TimeSpan timeAway)
    {
        float hoursAway = (float)timeAway.TotalHours;

        foreach (var monster in _playerConfig.ownedMonsters)
        {
            // Reduce happiness based on time away (example: -2% per hour)
            monster.currentHappiness -= hoursAway * 2f;
            monster.currentHappiness = Mathf.Clamp(monster.currentHappiness, 0f, 100f);

            // Reduce hunger over time (example: 5 units/hour)
            monster.currentHunger -= hoursAway * 5f;
            monster.currentHunger = Mathf.Clamp(monster.currentHunger, 0f, 100f);

            // Optional: Reduce health if hunger drops too low
            if (monster.currentHunger <= 40f)
            {
                monster.currentHealth -= hoursAway * 2f;
                monster.currentHealth = Mathf.Clamp(monster.currentHealth, 0f, 100f);
            }
        }
    }

    #endregion

    #region File Operations
    private static void LoadPlayerConfig()
    {
        string path = Path.Combine(Application.persistentDataPath, SaveFileName);

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                _playerConfig = JsonConvert.DeserializeObject<PlayerConfig>(json);
                _playerConfig.SyncFromSerializable();
                Debug.Log("Game data loaded successfully");
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load game data: " + e.Message);
                CreateNewPlayerConfig();
            }
        }
        else
        {
            CreateNewPlayerConfig();
        }
    }

    private static void SavePlayerConfig()
    {
        string path = Path.Combine(Application.persistentDataPath, SaveFileName);

        try
        {
            _playerConfig.SyncToSerializable(); // Convert DateTime to strings, etc.
            string json = JsonConvert.SerializeObject(_playerConfig, Formatting.Indented);
            File.WriteAllText(path, json);
            Debug.Log("Game data saved successfully");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save game data: " + e.Message);
        }
    }

    private static void CreateNewPlayerConfig()
    {
        _playerConfig = new PlayerConfig();
        _playerConfig.lastLoginTime = DateTime.Now;
        _playerConfig.SyncToSerializable();
        Debug.Log("Created new game data");
    }

    public static void DeleteAllSaveData()
    {
        string path = Path.Combine(Application.persistentDataPath, SaveFileName);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        _playerConfig = new PlayerConfig();
        _playerConfig.lastLoginTime = DateTime.Now;
        Debug.Log("All game data deleted");
    }
    public static void UpdateItemData(string itemID, ItemType category, int amount)
    {
        if (_playerConfig == null)
        {
            Debug.LogWarning("PlayerConfig is null, cannot update item data.");
            return;
        }

        _playerConfig.AddItem(itemID, category, amount);
        SaveAll();
    }
    #endregion
    
    #region Item Data Operation
    public static bool TryBuyItem(ItemDataSO itemData)
    {
        if (_playerConfig == null)
        {
            Debug.LogWarning("PlayerConfig is null, cannot buy item.");
            return false;
        }

        if (itemData == null)
        {
            Debug.LogWarning("ItemData is null.");
            return false;
        }

        int playerCoins = _playerConfig.coins;
        int itemPrice = itemData.price;

        if (playerCoins < itemPrice)
        {
            Debug.Log($"Not enough coins to buy {itemData.itemName}. Needed: {itemPrice}, Owned: {playerCoins}");
            return false;
        }

        // Deduct coins
        _playerConfig.coins -= itemPrice;

        // Add item to inventory
        _playerConfig.AddItem(itemData.itemID, itemData.category, 1);

        // Save changes
        SaveAll();

        Debug.Log($"Purchased {itemData.itemName} and {itemData.category}for {itemPrice} coins. Remaining: {_playerConfig.coins}");

        return true;
    }
    #endregion
    #region Biome Operations

    public static bool TryBuyBiome(string biomeID, int price)
    {
        if (_playerConfig == null)
        {
            Debug.LogWarning("PlayerConfig is null, cannot buy biome.");
            return false;
        }

        if (string.IsNullOrEmpty(biomeID))
        {
            Debug.LogWarning("Invalid biome ID.");
            return false;
        }

        if (_playerConfig.HasBiome(biomeID))
        {
            Debug.Log($"Biome {biomeID} already owned.");
            return true;
        }

        if (_playerConfig.coins < price)
        {
            Debug.Log($"Not enough coins to buy biome {biomeID}. Needed: {price}, Owned: {_playerConfig.coins}");
            return false;
        }

        _playerConfig.coins -= price;
        _playerConfig.AddOwnedBiome(biomeID);
        SaveAll();
        Debug.Log($"Bought biome {biomeID} for {price} coins. Remaining: {_playerConfig.coins}");
        return true;
    }

    public static void AddOwnedBiome(string biomeID)
    {
        if (string.IsNullOrEmpty(biomeID)) return;
        _playerConfig.AddOwnedBiome(biomeID);
        SaveAll();
    }

    public static bool IsBiomeOwned(string biomeID)
    {
        return _playerConfig.HasBiome(biomeID);
    }

    public static string GetActiveBiome()
    {
        return _playerConfig.activeBiomeID;
    }

    public static void SetActiveBiome(string biomeID)
    {
        // If blank or null, clear the active biome
        if (string.IsNullOrEmpty(biomeID))
        {
            _playerConfig.SetActiveBiome("");
            SaveAll();
            Debug.Log("Active biome cleared.");
            return;
        }

        // Otherwise, validate ownership before setting
        if (_playerConfig.HasBiome(biomeID))
        {
            _playerConfig.SetActiveBiome(biomeID);
            SaveAll();
        }
        else
        {
            Debug.LogWarning($"Attempted to set biome '{biomeID}' as active but it's not owned.");
        }
    }

    public static void SetSkyEnabled(bool enabled)
    {
        _playerConfig.isSkyEnabled = enabled;
        SaveAll();
    }

    public static bool IsSkyEnabled()
    {
        return _playerConfig.isSkyEnabled;
    }

    public static void SetCloudEnabled(bool enabled)
    {
        _playerConfig.isCloudEnabled = enabled;
        SaveAll();
    }

    public static bool IsCloudEnabled()
    {
        return _playerConfig.isCloudEnabled;
    }

    public static void SetAmbientEnabled(bool enabled)
    {
        _playerConfig.isAmbientEnabled = enabled;
        SaveAll();
    }

    public static bool IsAmbientEnabled()
    {
        return _playerConfig.isAmbientEnabled;
    }
    #endregion

    #region Game Area Operations
    public static void SaveActiveGameAreaIndex(int areaIndex)
    {
        if (_playerConfig == null)
        {
            Debug.LogWarning("PlayerConfig is null, cannot save game area index.");
            return;
        }

        _playerConfig.lastGameAreaIndex = areaIndex;
        SaveAll();
        Debug.Log($"Saved active game area index: {areaIndex}");
    }

    public static int LoadActiveGameAreaIndex()
    {
        if (_playerConfig == null)
        {
            Debug.LogWarning("PlayerConfig is null, returning default game area index 0.");
            return 0;
        }

        return _playerConfig.lastGameAreaIndex;
    }
    #endregion

    public static PlayerConfig GetPlayerConfig()
    {
        if (_playerConfig == null)
        {
            Debug.LogWarning("PlayerConfig was null, creating new one.");
            CreateNewPlayerConfig();
        }
        return _playerConfig;
    }

}
