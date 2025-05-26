using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MonsterDatabase", menuName = "Monster/Monster Database")]
public class MonsterDatabaseSO : ScriptableObject
{
    public List<MonsterDataSO> allMonsters;

    public MonsterDataSO GetMonsterByID(string id)
    {
        return allMonsters.Find(monster => monster.monID == id);
    }

    public MonsterDataSO GetRandomByRarity(MonsterType rarity)
    {
        var matching = allMonsters.FindAll(monster => monster.monType == rarity);
        if (matching.Count == 0) return null;
        return matching[Random.Range(0, matching.Count)];
    }
}
