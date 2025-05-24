using UnityEngine;
using System.Collections.Generic;

public static class SaveSystem
{
    // Pets
    public static void SavePet(PetData data)
    {
        string key = $"Pet{data.petName}";
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(key, json);
    }

    public static bool TryLoadPet(string petID, out PetData data)
    {
        string key = $"Pet{petID}";
        if (PlayerPrefs.HasKey(key))
        {
            data = JsonUtility.FromJson<PetData>(PlayerPrefs.GetString(key));
            return true;
        }
        data = null;
        return false;
    }

    // Global game state
    private const string MoneyKey      = "Money";
    private const string SavedPetsKey  = "SavedPetIDs";

    public static int LoadMoney() => PlayerPrefs.GetInt(MoneyKey, 100);
    public static void SaveMoney(int money) => PlayerPrefs.SetInt(MoneyKey, money);

    public static List<string> LoadSavedPetIDs()
    {
        string csv = PlayerPrefs.GetString(SavedPetsKey, "");
        return string.IsNullOrEmpty(csv)
            ? new List<string>()
            : new List<string>(csv.Split(','));
    }

    public static void SavePetIDs(List<string> ids)
    {
        PlayerPrefs.SetString(SavedPetsKey, string.Join(",", ids));
    }

    public static void Flush() => PlayerPrefs.Save();
}
