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
    {        if (monsterDatabase == null)
        {
            return;
        }

        var invalidWeights = rarityWeights.Where(w => !allowedRarities.Contains(w.type)).ToList();
        
        foreach (var rarity in allowedRarities)
        {
            var monstersOfRarity = monsterDatabase.monsters.Where(m => m.monType == rarity).Count();
        }
    }    public void RollGacha()
    {
        if (ServiceLocator.Get<GameManager>().coinCollected < gachaCost)
        {
            ServiceLocator.Get<UIManager>().ShowMessage("Not enough coins for gacha!", 1f);
            return;
        }

        MonsterType chosenRarity = GetRandomRarity();
        MonsterDataSO selectedMonster = SelectRandomMonster(chosenRarity);
        
        if (selectedMonster == null)
        {
            ServiceLocator.Get<UIManager>().ShowMessage("No monsters available!", 1f);
            return;
        }

        // Only spend coins AFTER we confirm we can spawn a monster
        if (ServiceLocator.Get<GameManager>().SpentCoin(gachaCost))
        {
            SpawnMonster(selectedMonster);
            ShowGachaResult(selectedMonster);
        }
    }

    private MonsterDataSO SelectRandomMonster(MonsterType rarity)
    {
        List<MonsterDataSO> candidates = monsterDatabase.monsters
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

    private void SpawnMonster(MonsterDataSO monsterData)
    {
        // Pass the monster data instead of just ID
        ServiceLocator.Get<GameManager>().SpawnMonsterFromGacha(monsterData);
    }

    private void ShowGachaResult(MonsterDataSO monster)
    {
        ServiceLocator.Get<UIManager>().ShowMessage(
            $"You got: {monster.monsterName} ({monster.monType})", 2f);
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
