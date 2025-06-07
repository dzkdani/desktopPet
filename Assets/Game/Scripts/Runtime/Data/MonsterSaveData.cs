using System;

[Serializable]
public class MonsterSaveData
{
    public string monsterId;
    public float lastHunger;
    public float lastHappiness;
    public float lastLowHungerTime;
    public bool isSick;
    public bool isEvolved;
    public bool isFinalForm;
    public int evolutionLevel;
    
    // Evolution tracking data - CLEANED UP
    public float timeSinceCreation;
    public int foodConsumed;
    public int interactionCount;
}