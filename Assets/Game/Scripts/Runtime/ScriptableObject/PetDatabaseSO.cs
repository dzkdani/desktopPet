using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PetDatabase", menuName = "Pet/Pet Database")]
public class PetDatabaseSO : ScriptableObject
{
    public List<MonsterDataSO> allPets;

    public MonsterDataSO GetPetByID(string id)
    {
        return allPets.Find(pet => pet.monID == id);
    }

    public MonsterDataSO GetRandomByRarity(MonsterType rarity)
    {
        var matching = allPets.FindAll(pet => pet.monType == rarity);
        if (matching.Count == 0) return null;
        return matching[Random.Range(0, matching.Count)];
    }
}
