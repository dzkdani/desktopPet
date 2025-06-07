using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

public static class SaveSystem
{
    private const string SaveFileName = "playerConfig.json";
    private static PlayerConfig _playerConfig;
    private static DateTime _sessionStartTime;
    // Global game state
    private const string CoinKey = "Coin";
    private const string PoopKey = "Poop";
    private const string MonsterKey = "MonsterIDs";

    public static void SaveCoin(int money) => PlayerPrefs.SetInt(CoinKey, money);
    public static int LoadCoin() => PlayerPrefs.GetInt(CoinKey, 100);
    public static void SavePoop(int poop) => PlayerPrefs.SetInt(PoopKey, poop);
    public static int LoadPoop() => PlayerPrefs.GetInt(PoopKey, 0);

    public static void Initialize()
    {
        LoadPlayerConfig();
        _sessionStartTime = DateTime.Now;

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
    // Pets
    public static void SaveMon(MonsterSaveData data)
    {
        string key = $"Pet{data.monsterId}";
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(key, json);

        // Save the ID to the master list
        SaveMonsterIDToList(data.monsterId);
        PlayerPrefs.Save();
    }

    public static bool LoadMon(string petID, out MonsterSaveData data)
    {
        string key = $"Pet{petID}";
        if (PlayerPrefs.HasKey(key))
        {
            data = JsonUtility.FromJson<MonsterSaveData>(PlayerPrefs.GetString(key));
            return true;
        }

        data = null;
        return false;
    }

    public static void DeleteMon(string monsterID)
    {
        // Delete the monster data
        string key = $"Pet{monsterID}";
        PlayerPrefs.DeleteKey(key);

        // Remove from master list
        List<string> existingIDs = LoadSavedMonIDs();
        if (existingIDs.Contains(monsterID))
        {
            existingIDs.Remove(monsterID);
            SaveMonIDs(existingIDs);
        }

        PlayerPrefs.Save();
    }

    private static void SaveMonsterIDToList(string monsterID)
    {
        List<string> existingIDs = LoadSavedMonIDs();
        if (!existingIDs.Contains(monsterID))
        {
            existingIDs.Add(monsterID);
            SaveMonIDs(existingIDs);
        }
    }

    public static void SaveMonIDs(List<string> ids)
    {
        PlayerPrefs.SetString(MonsterKey, string.Join(",", ids));
    }

    public static List<string> LoadSavedMonIDs()
    {
        string csv = PlayerPrefs.GetString(MonsterKey, "");
        return string.IsNullOrEmpty(csv)
            ? new List<string>()
            : new List<string>(csv.Split(','));
    }

    public static void Flush() => PlayerPrefs.Save();

    public static void ClearSaveData()
    {
        PlayerPrefs.DeleteKey(CoinKey);
        PlayerPrefs.DeleteKey(PoopKey);
        PlayerPrefs.DeleteKey(MonsterKey);

        // Clear all pet data
        var keys = PlayerPrefs.GetString(MonsterKey, "").Split(',');
        foreach (var key in keys)
        {
            if (!string.IsNullOrEmpty(key))
                PlayerPrefs.DeleteKey($"Pet{key}");
        }

        PlayerPrefs.Save();
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
            HandleLongAbsence(timeSinceLastLogin);
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
        // Update monster states based on time away
        foreach (var monster in _playerConfig.monsters.Values)
        {
            // Example: reduce happiness over time
            float hoursAway = (float)timeAway.TotalHours;
            // monster.happiness = Mathf.Clamp01(monster.happiness - (hoursAway * 0.05f));

            // You could add more effects like hunger, etc.
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
                _playerConfig = JsonUtility.FromJson<PlayerConfig>(json);
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
            string json = JsonUtility.ToJson(_playerConfig, true);
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
    #endregion
    #region Farming System Integration


    public static void SavePlants(PlantListWrapper plants)
    {
        string path = Path.Combine(Application.persistentDataPath, "plants.json");
        try
        {
            string json = JsonUtility.ToJson(plants, true);
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save plants: " + e.Message);
        }
    }

    public static PlantListWrapper LoadPlants()
    {
        string path = Path.Combine(Application.persistentDataPath, "plants.json");
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<PlantListWrapper>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load plants: " + e.Message);
            }
        }
        return null;
    }
    #endregion
    //  #region Player Stats
    // public static int Coins
    // {
    //     get => _playerConfig.coins;
    //     set => _playerConfig.coins = value;
    // }

    // public static int Poop
    // {
    //     get => _playerConfig.poop;
    //     set => _playerConfig.poop = value;
    // }
    // #endregion

    // #region Monster Management
    // public static void SaveMonster(MonsterSaveData data)
    // {
    //     if (_playerConfig.monsters.ContainsKey(data.monsterId))
    //     {
    //         _playerConfig.monsters[data.monsterId] = data;
    //     }
    //     else
    //     {
    //         _playerConfig.monsters.Add(data.monsterId, data);
    //         _playerConfig.monsterIDs.Add(data.monsterId);
    //     }
    // }

    // public static bool TryGetMonster(string monsterId, out MonsterSaveData data)
    // {
    //     return _playerConfig.monsters.TryGetValue(monsterId, out data);
    // }

    // public static void DeleteMonster(string monsterId)
    // {
    //     if (_playerConfig.monsters.ContainsKey(monsterId))
    //     {
    //         _playerConfig.monsters.Remove(monsterId);
    //         _playerConfig.monsterIDs.Remove(monsterId);
    //     }
    // }

    // public static List<string> GetAllMonsterIDs()
    // {
    //     return new List<string>(_playerConfig.monsterIDs);
    // }
    // #endregion
}
[Serializable]
public class PlantListWrapper
{
    public List<PlantSaveData> plants;
}
