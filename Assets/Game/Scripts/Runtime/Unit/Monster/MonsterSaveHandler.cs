using UnityEngine;

public class MonsterSaveHandler
{
    private MonsterController _controller;
    
    public MonsterSaveHandler(MonsterController controller)
    {
        _controller = controller;
    }
    
    public void SaveData()
    {
        var data = new MonsterSaveData
        {
            monsterId = _controller.monsterID,
            lastHunger = _controller.currentHunger,
            lastHappiness = _controller.currentHappiness,
            isFinalForm = _controller.isFinalForm,
            evolutionLevel = _controller.evolutionLevel,
            
            // Sick status data
            isSick = _controller.IsSick,
            lastLowHungerTime = _controller.GetLowHungerTime(), // You'll need to add this getter
            
            // Evolution data - get directly from evolution handler
            timeSinceCreation = _controller.GetEvolutionTimeSinceCreation(),
            foodConsumed = _controller.GetEvolutionFoodConsumed(),
            interactionCount = _controller.GetEvolutionInteractionCount()
        };
        
        SaveSystem.SaveMon(data);
        Debug.Log($"[Monster] Saving monster data for {_controller.monsterID}: Hunger={data.lastHunger:F1}, Happiness={data.lastHappiness:F1}, IsSick={data.isSick}, LowHungerTime={data.lastLowHungerTime:F1}s");
    }
    
    public void LoadData()
    {
        if (SaveSystem.LoadMon(_controller.monsterID, out var data))
        {
            Debug.Log($"[Monster] Loading existing monster data for {_controller.monsterID}: Hunger={data.lastHunger:F1}, Happiness={data.lastHappiness:F1}, IsSick={data.isSick}, LowHungerTime={data.lastLowHungerTime:F1}s");
            
            // Load basic stats
            _controller.SetHunger(data.lastHunger);
            _controller.SetHappiness(data.lastHappiness);
            _controller.isFinalForm = data.isFinalForm;
            _controller.evolutionLevel = data.evolutionLevel;
            
            // Load sick status data
            _controller.SetSick(data.isSick);
            _controller.SetLowHungerTime(data.lastLowHungerTime); // You'll need to add this setter
            
            // Load evolution data
            _controller.LoadEvolutionData(
                data.timeSinceCreation,
                data.foodConsumed,
                data.interactionCount
            );
            
            // Update visuals after loading
            _controller.UpdateVisuals();
        }
        else
        {
            Debug.Log($"[Monster] No existing save data found for {_controller.monsterID}, initializing as new monster");
            InitNewMonster();
        }
        
        ApplyMonsterDataStats();
    }
    
    private void InitNewMonster()
    {
        var monsterData = _controller.MonsterData;
        float baseHunger = monsterData != null ? monsterData.baseHunger : 50f;
        float baseHappiness = monsterData != null ? monsterData.baseHappiness : 0f;
        
        Debug.Log($"[Monster] Initializing new monster {_controller.monsterID} with base values: Hunger={baseHunger}, Happiness={baseHappiness}");
        
        _controller.SetHunger(baseHunger);
        _controller.SetHappiness(baseHappiness);
        
        // Initialize sick status for new monsters
        _controller.SetSick(false);
        _controller.SetLowHungerTime(0f);
        
        if (monsterData != null)
        {
            monsterData.isEvolved = false;
            monsterData.isFinalEvol = false;
            monsterData.evolutionLevel = 0;
        }
    }
    
    private void ApplyMonsterDataStats()
    {
        var monsterData = _controller.MonsterData;
        if (monsterData == null) return;

        _controller.stats.moveSpeed = monsterData.moveSpd;
        _controller.stats.hungerDepletionRate = monsterData.hungerDepleteRate;
        _controller.stats.poopInterval = monsterData.poopRate;
        _controller.stats.pokeHappinessIncrease = monsterData.pokeHappinessValue;
        _controller.stats.areaHappinessRate = monsterData.areaHappinessRate;
    }
}