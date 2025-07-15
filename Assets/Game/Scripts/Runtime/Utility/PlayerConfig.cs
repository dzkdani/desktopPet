using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class PlayerConfig
{
    public int coins = 10000;
    public int poops = 0;
    public int lastGameAreaIndex = 0; // Default to first game area
    public int maxGameArea = 1; // Tracks the highest game area index created

    public string lastLoginTimeString;
    public string totalPlayTimeString;

    [NonSerialized] public DateTime lastLoginTime;
    [NonSerialized] public TimeSpan totalPlayTime;

    public List<GameAreaData> gameAreas = new(); // List of game areas
    public List<MonsterSaveData> ownedMonsters = new(); // Now using List for full JsonUtility support
    public List<NPCSaveData> ownedNPCMonsters = new(); // For monsters that are owned but not in the world
    public List<OwnedItemData> ownedItems = new();
    public List<string> ownedBiomes = new();
    public string activeBiomeID = "default_biome";
    public bool isSkyEnabled = false;
    public bool isCloudEnabled = false;
    public bool isAmbientEnabled = false;
    public bool isRainEnabled = false;

    // Serialization Sync
    public void SyncToSerializable()
    {
        lastLoginTimeString = lastLoginTime.ToString("o");
        totalPlayTimeString = totalPlayTime.ToString();
    }

    public void SyncFromSerializable()
    {
        DateTime.TryParse(lastLoginTimeString, null, System.Globalization.DateTimeStyles.RoundtripKind, out lastLoginTime);
        TimeSpan.TryParse(totalPlayTimeString, out totalPlayTime);
    }

    // Inventory Logic
    public void AddItem(string itemID, ItemType type, int amount)
    {
        if (amount == 0 || string.IsNullOrEmpty(itemID)) return;

        var item = ownedItems.Find(i => i.itemID == itemID);
        if (item == null)
        {
            ownedItems.Add(new OwnedItemData { itemID = itemID, type = type, amount = Mathf.Max(0, amount) });
        }
        else
        {
            item.amount = Mathf.Max(0, item.amount + amount);
            if (item.amount == 0)
                ownedItems.Remove(item);
        }
    }

    public void RemoveItem(string itemID, int amount)
    {
        if (amount <= 0 || string.IsNullOrEmpty(itemID)) return;

        var existing = ownedItems.Find(i => i.itemID == itemID);
        if (existing != null)
        {
            existing.amount -= amount;
            if (existing.amount <= 0)
                ownedItems.Remove(existing);
        }
    }

    public int GetItemAmount(string itemID)
    {
        return ownedItems.Find(i => i.itemID == itemID)?.amount ?? 0;
    }

    // Monster Save Logic
    public void SaveMonsterData(MonsterSaveData data)
    {
        if (data == null || string.IsNullOrEmpty(data.instanceId)) return;

        var existing = ownedMonsters.Find(m => m.instanceId == data.instanceId);
        if (existing != null)
        {
            int index = ownedMonsters.IndexOf(existing);
            ownedMonsters[index] = data;
        }
        else
        {
            ownedMonsters.Add(data);
        }
    }

    public bool LoadMonsterData(string instanceId, out MonsterSaveData data)
    {
        data = ownedMonsters.Find(m => m.instanceId == instanceId);
        return data != null;
    }

    public void DeleteMonster(string instanceId)
    {
        ownedMonsters.RemoveAll(m => m.instanceId == instanceId);
    }

    public List<string> GetAllMonsterIDs()
    {
        return ownedMonsters.Select(m => m.instanceId).ToList();
    }

    public void SetAllMonsterIDs(List<string> ids)
    {
        ownedMonsters = ownedMonsters.Where(m => ids.Contains(m.instanceId)).ToList();
    }

    public void ClearAllMonsterData()
    {
        ownedMonsters.Clear();
    }

    public void SaveNPCMonsterData(NPCSaveData data)
    {
        if (data == null || string.IsNullOrEmpty(data.instanceId)) return;

        var existing = ownedNPCMonsters.Find(m => m.instanceId == data.instanceId);
        if (existing != null)
        {
            int index = ownedNPCMonsters.IndexOf(existing);
            ownedNPCMonsters[index] = data;
        }
        else
        {
            ownedNPCMonsters.Add(data);
        }
    }

    public bool LoadNPCMonsterData(string instanceId, out NPCSaveData data)
    {
        data = ownedNPCMonsters.Find(m => m.instanceId == instanceId);
        return data != null;
    }

    public void DeleteNPCMonster(string instanceId)
    {
        ownedNPCMonsters.RemoveAll(m => m.instanceId == instanceId);
    }

    public List<string> GetAllNPCMonsterIDs()
    {
        return ownedNPCMonsters.Select(m => m.instanceId).ToList();
    }

    public void SetAllNPCMonsterIDs(List<string> ids)
    {
        ownedNPCMonsters = ownedNPCMonsters.Where(m => ids.Contains(m.instanceId)).ToList();
    }

    public void ClearAllNPCMonsterData()
    {
        ownedNPCMonsters.Clear();
    }

    // Biome Logic
    public void AddOwnedBiome(string biomeID)
    {
        if (!ownedBiomes.Contains(biomeID))
            ownedBiomes.Add(biomeID);
    }

    public bool HasBiome(string biomeID)
    {
        // If empty, consider it always valid for default biome logic
        if (string.IsNullOrEmpty(biomeID))
            return true;

        return ownedBiomes.Contains(biomeID);
    }

    public void SetActiveBiome(string biomeID)
    {
        // Allow clearing the active biome with an empty string
        if (string.IsNullOrEmpty(biomeID) || HasBiome(biomeID))
        {
            activeBiomeID = biomeID;
        }
    }

    // Game Area specific monster operations
    public List<MonsterSaveData> GetMonstersForGameArea(int gameAreaIndex)
    {
        return ownedMonsters.Where(m => m.gameAreaId == gameAreaIndex).ToList();
    }

    public void SetMonsterGameArea(string instanceId, int gameAreaIndex)
    {
        var monster = ownedMonsters.Find(m => m.instanceId == instanceId);
        if (monster != null)
        {
            monster.gameAreaId = gameAreaIndex;
        }
    }

    public void MoveMonsterToGameArea(string instanceId, int fromArea, int toArea)
    {
        var monster = ownedMonsters.Find(m => m.instanceId == instanceId && m.gameAreaId == fromArea);
        if (monster != null)
        {
            monster.gameAreaId = toArea;
        }
    }

    public int GetMonsterCountForGameArea(int gameAreaIndex)
    {
        return ownedMonsters.Count(m => m.gameAreaId == gameAreaIndex);
    }

    public void DeleteMonstersFromGameArea(int gameAreaIndex)
    {
        ownedMonsters.RemoveAll(m => m.gameAreaId == gameAreaIndex);
    }
}

[Serializable]
public class OwnedItemData
{
    public string itemID;
    public ItemType type;
    public int amount;
}

[Serializable]
public class NPCSaveData
{
    public string instanceId;
    public string monsterId;
}

[Serializable]
public class GameAreaData
{
    public string name;
    public int index;
    public List<string> monsterIDs = new List<string>();
    public List<string> npcMonsterIDs = new List<string>();
}

[Serializable]
public class MonsterCollectionData
{
    public bool isUnlocked;
    public string monsterId;
    public string monsterName;
    public string monsterCount;
    public int evolutionStage;
}


