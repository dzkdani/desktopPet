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
            evolutionLevel = _controller.evolutionLevel
        };
        SaveSystem.SaveMon(data);
    }
    
    public void LoadData()
    {
        if (_controller.MonsterData == null)
        {
            InitNewMonster();
            return;
        }

        if (SaveSystem.LoadMon(_controller.monsterID, out MonsterSaveData savedData))
            LoadFromSaveData(savedData);
        else
            InitNewMonster();

        ApplyMonsterDataStats();
    }
    
    private void LoadFromSaveData(MonsterSaveData savedData)
    {
        _controller.SetHunger(savedData.lastHunger);
        _controller.SetHappiness(savedData.lastHappiness);
        _controller.monsterID = savedData.monsterId;
        
        _controller.isEvolved = savedData.isEvolved;
        _controller.isFinalForm = savedData.isFinalForm;
        _controller.evolutionLevel = savedData.evolutionLevel;
        
        if (_controller.MonsterData != null)
        {
            var loadedData = _controller.MonsterData;
            loadedData.isEvolved = savedData.isEvolved;
            loadedData.isFinalEvol = savedData.isFinalForm;
            loadedData.evolutionLevel = savedData.evolutionLevel;
            
            _controller.UpdateVisuals();
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