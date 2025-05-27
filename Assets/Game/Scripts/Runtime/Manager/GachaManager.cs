using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance;
    [System.Serializable]
    public class RarityWeight
    {
        public MonsterType type;
        public float weight;
    }

    public MonsterDatabaseSO monsterDatabase;
    public List<RarityWeight> rarityWeights;
    public int gachaCost = 10;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void RollGacha()
    {
        if (!GameManager.Instance.SpentCoin(gachaCost))
        {
            Debug.Log("Not enough coins for gacha!");
            return;
        }

        MonsterType chosenRarity = GetRandomRarity();
        List<MonsterDataSO> candidates = monsterDatabase.allMonsters
            .Where(m => m.monType != MonsterType.Boss) // Exclude Boss type
            .Where(m => m.monType == chosenRarity)
            .ToList();

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"No monsters available for rarity: {chosenRarity}");
            return;
        }

        MonsterDataSO selected = candidates[Random.Range(0, candidates.Count)];
        SpawnMonster(selected.monID);
        UIManager.Instance.ShowMessage($"You got: {selected.monName} ({selected.monType})", 2f);
    }

    private MonsterType GetRandomRarity()
    {
        float totalWeight = rarityWeights.Sum(r => r.weight);
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var rarity in rarityWeights)
        {
            cumulative += rarity.weight;
            if (roll <= cumulative)
                return rarity.type;
        }

        return rarityWeights[0].type; // fallback
    }

    private void SpawnMonster(string monsterID)
    {
        GameManager.Instance.SpawnLoadedMonsViaGacha(monsterID);
    }
}
