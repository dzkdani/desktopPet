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
            isEvolved = _controller.isEvolved,
            isFinalForm = _controller.isFinalForm,
            evolutionLevel = _controller.evolutionLevel,
            
            // Evolution data - get directly from evolution handler
            timeSinceCreation = _controller.GetEvolutionTimeSinceCreation(),
            totalHappinessAccumulated = _controller.GetEvolutionTotalHappiness(),
            totalHungerSatisfied = _controller.GetEvolutionTotalHunger(),
            foodConsumed = _controller.GetEvolutionFoodConsumed(),
            interactionCount = _controller.GetEvolutionInteractionCount()
        };
        
        SaveSystem.SaveMon(data);
    }
    
    public void LoadData()
    {
        if (SaveSystem.LoadMon(_controller.monsterID, out var data))
        {
            _controller.SetHunger(data.lastHunger);
            _controller.SetHappiness(data.lastHappiness);
            _controller.isEvolved = data.isEvolved;
            _controller.isFinalForm = data.isFinalForm;
            _controller.evolutionLevel = data.evolutionLevel;
            
            // Load evolution data directly
            _controller.LoadEvolutionData(
                data.timeSinceCreation,
                data.totalHappinessAccumulated,
                data.totalHungerSatisfied,
                data.foodConsumed,
                data.interactionCount
            );
        }
    }
    
    private void InitNewMonster()
    {
        var monsterData = _controller.MonsterData;
        float baseHunger = monsterData != null ? monsterData.baseHunger : 50f;
        float baseHappiness = monsterData != null ? monsterData.baseHappiness : 0f;
        
        _controller.SetHunger(baseHunger);
        _controller.SetHappiness(baseHappiness);
        
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