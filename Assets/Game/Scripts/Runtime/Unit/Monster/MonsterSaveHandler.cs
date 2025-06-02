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
            
            // Evolution data - get directly from evolution handler
            timeSinceCreation = _controller.GetEvolutionTimeSinceCreation(),
            foodConsumed = _controller.GetEvolutionFoodConsumed(),
            interactionCount = _controller.GetEvolutionInteractionCount()
        };
        
        SaveSystem.SaveMon(data);
        Debug.Log($"[Evolution] Saving monster data for {_controller.monsterID}: Level={data.evolutionLevel}, Time={data.timeSinceCreation:F1}s, Food={data.foodConsumed}, Interactions={data.interactionCount}");
    }
    
    public void LoadData()
    {
        if (SaveSystem.LoadMon(_controller.monsterID, out var data))
        {
            Debug.Log($"[Evolution] Loading existing monster data for {_controller.monsterID}: Level={data.evolutionLevel}, Time={data.timeSinceCreation:F1}s, Food={data.foodConsumed}, Interactions={data.interactionCount}");
            
            // Existing monster - load saved data
            _controller.SetHunger(data.lastHunger);
            _controller.SetHappiness(data.lastHappiness);
            _controller.isFinalForm = data.isFinalForm;
            _controller.evolutionLevel = data.evolutionLevel;
            
            _controller.LoadEvolutionData(
                data.timeSinceCreation,
                data.foodConsumed,
                data.interactionCount
            );
            
            // IMPORTANT: Update visuals after loading evolution level
            _controller.UpdateVisuals();
        }
        else
        {
            Debug.Log($"[Evolution] No existing save data found for {_controller.monsterID}, initializing as new monster");
            // New monster - initialize with base values from MonsterDataSO
            InitNewMonster();
        }
        
        // Always apply monster data stats
        ApplyMonsterDataStats();
    }
    
    private void InitNewMonster()
    {
        var monsterData = _controller.MonsterData;
        float baseHunger = monsterData != null ? monsterData.baseHunger : 50f;
        float baseHappiness = monsterData != null ? monsterData.baseHappiness : 0f;
        
        Debug.Log($"[Evolution] Initializing new monster {_controller.monsterID} with base values: Hunger={baseHunger}, Happiness={baseHappiness}");
        
        _controller.SetHunger(baseHunger);
        _controller.SetHappiness(baseHappiness);
        
        if (monsterData != null)
        {
            monsterData.isEvolved = false;
            monsterData.isFinalEvol = false;
            monsterData.evolutionLevel = 0;
            Debug.Log($"[Evolution] Set monster data evolution state: Level=0, IsEvolved=false, IsFinal=false");
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