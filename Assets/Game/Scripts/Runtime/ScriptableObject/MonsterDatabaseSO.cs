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

    // Add validation method for editor
    private void OnValidate()
    {
        ValidateMonsterIDs();
    }

    private void ValidateMonsterIDs()
    {
        if (monsters == null) return;        for (int i = 0; i < monsters.Count; i++)
        {
            if (monsters[i] != null && string.IsNullOrEmpty(monsters[i].id))
            {
            }
        }
    }
}
