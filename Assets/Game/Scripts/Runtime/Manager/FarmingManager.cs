using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FarmingManager : MonoBehaviour
{

    private Dictionary<string, PlantSaveData> _plants = new Dictionary<string, PlantSaveData>();

    private void Awake()
    {
        ServiceLocator.Register(this);
    }

    private void Start()
    {
        LoadPlants();
        UpdateAllPlants();
    }

    private void OnApplicationQuit()
    {
        SaveAllPlants();
    }

    #region Plant Management
    public void SavePlant(PlantSaveData plant)
    {
        if (_plants.ContainsKey(plant.plantId))
        {
            _plants[plant.plantId] = plant;
        }
        else
        {
            _plants.Add(plant.plantId, plant);
        }
    }

    public bool TryGetPlant(string plantId, out PlantSaveData plant)
    {
        return _plants.TryGetValue(plantId, out plant);
    }

    public List<PlantSaveData> GetAllPlants()
    {
        return new List<PlantSaveData>(_plants.Values);
    }

    public void WaterPlant(string plantId)
    {
        if (_plants.TryGetValue(plantId, out PlantSaveData plant))
        {
            plant.lastWateredTime = DateTime.Now;
            plant.waterLevel = 1f; // Fully watered
            SavePlant(plant);
        }
    }

    public void PlantNewSeed(string plantType)
    {
        var newPlant = new PlantSaveData
        {
            plantId = Guid.NewGuid().ToString(),
            plantType = plantType,
            currentState = PlantState.Seed,
            growthProgress = 0f,
            lastWateredTime = DateTime.Now,
            plantedTime = DateTime.Now,
            waterLevel = 1f
        };
        SavePlant(newPlant);
    }
    void OnDestroy()
    {
        ServiceLocator.Unregister<FarmingManager>();
    }

    public void UpdateAllPlants()
    {
        bool anyChanges = false;
        DateTime currentTime = DateTime.Now;

        foreach (var plant in _plants.Values)
        {
            if (plant.currentState == PlantState.Mature || plant.currentState == PlantState.Withered)
                continue;

            TimeSpan timeSinceWatered = currentTime - plant.lastWateredTime;

            // Calculate water depletion
            float waterDepletionRate = GetWaterDepletionRate(plant.plantType);
            plant.waterLevel = Mathf.Clamp01(plant.waterLevel - (float)(timeSinceWatered.TotalHours * waterDepletionRate));

            // Update growth based on water level
            float growthRate = GetGrowthRate(plant.plantType);
            float effectiveGrowthRate = growthRate * plant.waterLevel;
            plant.growthProgress = Mathf.Clamp01(plant.growthProgress + (float)(timeSinceWatered.TotalHours * effectiveGrowthRate / 24f));

            // Check for state changes
            if (plant.waterLevel <= 0f && plant.currentState != PlantState.Withered)
            {
                plant.currentState = PlantState.Withered;
                anyChanges = true;
            }
            else if (plant.growthProgress >= 1f && plant.currentState != PlantState.Mature)
            {
                plant.currentState = PlantState.Mature;
                anyChanges = true;
            }
            else if (plant.growthProgress > 0.1f && plant.currentState == PlantState.Seed)
            {
                plant.currentState = PlantState.Growing;
                anyChanges = true;
            }
        }

        if (anyChanges)
        {
            SaveAllPlants();
        }
    }

    private float GetWaterDepletionRate(string plantType)
    {
        // Return water depletion rate per hour
        return 0.05f; // 5% per hour
    }

    private float GetGrowthRate(string plantType)
    {
        // Return base growth rate
        return 1.0f; // 1x normal speed
    }
    #endregion

    #region Save/Load Integration
    private void LoadPlants()
    {
        var plantList = SaveSystem.LoadPlants();
        if (plantList != null)
        {
            _plants = plantList.plants.ToDictionary(p => p.plantId, p => p);
        }
    }

    private void SaveAllPlants()
    {
        var wrapper = new PlantListWrapper { plants = _plants.Values.ToList() };
        SaveSystem.SavePlants(wrapper);
    }
    #endregion
}