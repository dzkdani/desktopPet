using System;

[Serializable]
public enum PlantState
{
    Seed,
    Growing,
    Mature,
    Withered
}

[Serializable]
public class PlantSaveData
{
    public string plantId;
    public string plantType;
    public PlantState currentState;
    public float growthProgress; // 0-1
    public DateTime lastWateredTime;
    public DateTime plantedTime;
    public float waterLevel; // 0-1
}