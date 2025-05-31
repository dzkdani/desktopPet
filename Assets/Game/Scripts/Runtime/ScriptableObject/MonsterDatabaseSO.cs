using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MonsterDatabase", menuName = "Monster/Monster Database")]
public class MonsterDatabaseSO : ScriptableObject
{
    public List<MonsterDataSO> monsters; 

    public MonsterDataSO GetMonsterByID(string id)
    {
        return monsters.Find(monster => monster.id == id);
    }
    public List<MonsterDataSO> GetMonstersByType(MonsterType type)
    {
        return monsters.FindAll(monster => monster.monType == type);
    }
}
