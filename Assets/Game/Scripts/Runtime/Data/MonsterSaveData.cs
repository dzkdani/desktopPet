using System;

[Serializable]
public class MonsterSaveData
{
    public string monsterId;
    public float lastHunger;
    public float lastHappiness;
    public bool isEvolved;
    public bool isFinalForm;
    public int evolutionLevel;
    
    // Evolution tracking data
    public float timeSinceCreation;
    public float totalHappinessAccumulated;
    public float totalHungerSatisfied;
    public int foodConsumed;
    public int interactionCount;
}