using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class RarityWeight
{
    public MonsterType type;
    [Range(0f, 100f)]
    public float weight;
}

public class GachaManager : MonoBehaviour
{
    [Header("Gacha Configuration")]
    public MonsterDatabaseSO monsterDatabase;
    public List<RarityWeight> rarityWeights;
    public int gachaCost = 10;
    
    [Header("Allowed Rarities")]
    [SerializeField] private List<MonsterType> allowedRarities = new List<MonsterType>
    {
        MonsterType.Rare,
        MonsterType.Mythic,
        MonsterType.Boss
    };

    private void Awake()
    {
        ServiceLocator.Register(this);
        ValidateConfiguration();
    }

    private void ValidateConfiguration()
    {
        if (monsterDatabase == null)
        {
            Debug.LogError("MonsterDatabase is not assigned!");
            return;
        }

        // Validate that rarityWeights only contains allowed rarities
        var invalidWeights = rarityWeights.Where(w => !allowedRarities.Contains(w.type)).ToList();
        if (invalidWeights.Count > 0)
        {
            Debug.LogWarning($"RarityWeights contains non-allowed rarities: {string.Join(", ", invalidWeights.Select(w => w.type))}");
        }

        // Check if we have monsters for each allowed rarity
        foreach (var rarity in allowedRarities)
        {
            var monstersOfRarity = monsterDatabase.allMonsters.Where(m => m.monType == rarity).Count();
            if (monstersOfRarity == 0)
            {
                Debug.LogWarning($"No monsters found for rarity: {rarity}");
            }
        }
    }

    public void RollGacha()
    {
        if (!CanAffordGacha())
        {
            Debug.Log("Not enough coins for gacha!");
            return;
        }

        MonsterType chosenRarity = GetRandomRarity();
        MonsterDataSO selectedMonster = SelectRandomMonster(chosenRarity);
        
        if (selectedMonster == null)
        {
            Debug.LogWarning($"No monsters available for rarity: {chosenRarity}");
            return;
        }

        SpawnMonster(selectedMonster.monID);
        ShowGachaResult(selectedMonster);
    }

    private bool CanAffordGacha()
    {
        return ServiceLocator.Get<GameManager>().SpentCoin(gachaCost);
    }

    private MonsterDataSO SelectRandomMonster(MonsterType rarity)
    {
        List<MonsterDataSO> candidates = monsterDatabase.allMonsters
            .Where(m => allowedRarities.Contains(m.monType))
            .Where(m => m.monType == rarity)
            .ToList();

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    private MonsterType GetRandomRarity()
    {
        // Filter weights to only include allowed rarities
        var validWeights = rarityWeights.Where(w => allowedRarities.Contains(w.type)).ToList();
        
        if (validWeights.Count == 0)
        {
            Debug.LogError("No valid rarity weights found!");
            return allowedRarities[0];
        }

        float totalWeight = validWeights.Sum(r => r.weight);
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var rarity in validWeights)
        {
            cumulative += rarity.weight;
            if (roll <= cumulative)
                return rarity.type;
        }

        return validWeights[0].type; // fallback
    }

    private void SpawnMonster(string monsterID)
    {
        ServiceLocator.Get<GameManager>().SpawnLoadedMonsViaGacha(monsterID);
    }

    private void ShowGachaResult(MonsterDataSO monster)
    {
        ServiceLocator.Get<UIManager>().ShowMessage(
            $"You got: {monster.monName} ({monster.monType})", 2f);
    }

    // Public method to add new allowed rarities at runtime if needed
    public void AddAllowedRarity(MonsterType rarity)
    {
        if (!allowedRarities.Contains(rarity))
        {
            allowedRarities.Add(rarity);
        }
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<GachaManager>();
    }
}
