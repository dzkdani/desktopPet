using System;

[System.Serializable]
public class MonsterSaveData
{
    public string monsterId;
    public float lastHunger;
    public float lastHappiness;
    public bool isEvolved;
    public bool isFinalForm;
    public int evolutionLevel;
}