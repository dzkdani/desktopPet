using UnityEngine;
using System.Collections.Generic;

public static class SaveSystem
{
    // Global game state
    private const string CoinKey = "Coin";
    private const string PoopKey = "Poop";
    private const string MonsterKey = "MonsterIDs";

    public static void SaveCoin(int money) => PlayerPrefs.SetInt(CoinKey, money);
    public static int LoadCoin() => PlayerPrefs.GetInt(CoinKey, 100);
    public static void SavePoop(int poop) => PlayerPrefs.SetInt(PoopKey, poop);
    public static int LoadPoop() => PlayerPrefs.GetInt(PoopKey, 0);

    // Pets
    public static void SaveMon(MonsterSaveData data)
    {
        string key = $"Pet{data.monsterId}";
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(key, json);
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
}                                                                                                                                                                                       
       