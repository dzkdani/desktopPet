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
    public List<MonsterDataSO> GetMonstersByType(MonsterType type)
    {
        return allMonsters.FindAll(monster => monster.monType == type);
    }
}
