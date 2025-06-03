using UnityEngine;
using System;
using System.Linq;

[Serializable]
public class MonsterEvolutionSaveData
{
    public string monsterId;
    public float timeSinceCreation;
    public int foodConsumed;
    public int interactionCount;
}

public class MonsterEvolutionHandler
{
    private EvolutionRequirementsSO _evolutionConfig;
    
    private MonsterController _controller;
    
    // Evolution tracking
    private float _lastUpdateTime;
    private float _lastEvolutionTime = -1f;
    // private float _evolutionCooldown = 3600f; // 1 hour cooldown

    private float _timeSinceCreation;
    private int _foodConsumed;
    private int _interactionCount;

    public bool CanEvolve => _controller?.MonsterData != null && _controller.MonsterData.canEvolve && !_controller.MonsterData.isFinalEvol;
    public float TimeSinceCreation => _timeSinceCreation;
    public int FoodConsumed => _foodConsumed;
    public int InteractionCount => _interactionCount;    public MonsterEvolutionHandler(MonsterController controller)
    {
        _controller = controller;
        InitializeEvolutionRequirements();
    }    private void InitializeEvolutionRequirements()
    {
        if (_controller == null)
        {
            Debug.LogWarning("[Evolution] Controller is null, cannot initialize evolution requirements");
            return;
        }
        
        if (_controller.MonsterData == null)
        {
            Debug.LogWarning($"[Evolution] MonsterData is null for {_controller.monsterID}, cannot initialize evolution requirements");
            return; 
        }
        
        if (_controller.MonsterData.evolutionRequirements == null)
        {
            Debug.LogWarning($"[Evolution] No evolution requirements found for {_controller.MonsterData.monsterName} ({_controller.monsterID})");
            return;
        }
          _evolutionConfig = _controller.MonsterData.evolutionRequirements;
    }
    
    private EvolutionRequirement[] GetAvailableEvolutions()
    {
        if (_evolutionConfig == null || _evolutionConfig.requirements == null)
        {
            Debug.LogWarning($"[Evolution] No evolution config available for {_controller?.monsterID}");
            return new EvolutionRequirement[0];
        }
          var available = _evolutionConfig.requirements
            .Where(req => req.targetEvolutionLevel == _controller.evolutionLevel + 1)
            .ToArray();
        
        return available;
    }    public void LoadEvolutionData(float timeSinceCreation, int foodConsumed, int interactionCount)
    {
        _timeSinceCreation = timeSinceCreation;
        _foodConsumed = foodConsumed;
        _interactionCount = interactionCount;
    }

    public void UpdateEvolutionTracking(float deltaTime)
    {
        if (!CanEvolve || _controller?.MonsterData == null) return;
        
        _timeSinceCreation += deltaTime;
          if (Time.time - _lastUpdateTime >= 5f)
        {
            CheckEvolutionConditions();
            _lastUpdateTime = Time.time;
        }
    }    public void OnFoodConsumed()
    {
        _foodConsumed++;
        CheckEvolutionConditions();
    }    public void OnInteraction()
    {
        _interactionCount++;
        CheckEvolutionConditions();
    }    private void CheckEvolutionConditions()
    {
        // Check cooldown
        // if (Time.time - _lastEvolutionTime < _evolutionCooldown)
        // {
        //     float remainingCooldown = _evolutionCooldown - (Time.time - _lastEvolutionTime);
        //     return;
        // }

        if (!CanEvolve)
        {
            return;
        }

        var nextEvolution = GetNextEvolutionRequirement();        if (nextEvolution == null)
        {
            Debug.LogWarning($"[Evolution] No next evolution requirement found for {_controller?.monsterID} at level {_controller?.evolutionLevel}");
            return;
        }
        
        if (MeetsEvolutionRequirements(nextEvolution))
        {
            TriggerEvolution();
        }
    }    private EvolutionRequirement GetNextEvolutionRequirement()
    {
        int currentLevel = _controller.evolutionLevel;

        foreach (var requirement in GetAvailableEvolutions())
        {
            if (requirement.targetEvolutionLevel == currentLevel + 1)
            {
                return requirement;
            }
        }

        Debug.LogWarning($"[Evolution] No evolution requirement found for level {currentLevel + 1}");
        return null;
    }    private bool MeetsEvolutionRequirements(EvolutionRequirement requirement)
    {
        bool timeCheck = _timeSinceCreation >= requirement.minTimeAlive;
        if (!timeCheck) return false;
        
        bool foodCheck = _foodConsumed >= requirement.minFoodConsumed;
        if (!foodCheck) return false;
        
        bool interactionCheck = _interactionCount >= requirement.minInteractions;
        if (!interactionCheck) return false;
        
        bool happinessCheck = _controller.currentHappiness >= requirement.minCurrentHappiness;
        if (!happinessCheck) return false;
        
        bool hungerCheck = _controller.currentHunger >= requirement.minCurrentHunger;
        if (!hungerCheck) return false;

        bool customCheck = requirement.customCondition?.Invoke(_controller) ?? true;
        
        return customCheck;
    }

    public void TriggerEvolution()
    {
        if (!CanEvolve)
        {
            Debug.LogWarning($"[Evolution] Cannot trigger evolution for {_controller?.monsterID} - CanEvolve is false");
            return;
        }        var oldLevel = _controller.evolutionLevel;
        var newLevel = oldLevel + 1;
        
        // Start simple evolution effect
        StartSimpleEvolutionEffect(oldLevel, newLevel);
    }

    private void StartSimpleEvolutionEffect(int oldLevel, int newLevel)
    {
        // Apply evolution changes directly
        _controller.evolutionLevel = newLevel;
        UpdateMonsterID(newLevel);
        _controller.UpdateVisuals();
        OnEvolutionComplete(oldLevel, newLevel);
    }

    private void UpdateMonsterID(int newLevel)
    {
        var oldID = _controller.monsterID;
        var parts = _controller.monsterID.Split('_');        if (parts.Length >= 3)
        {
            // Update the existing ID format
            _controller.monsterID = $"{parts[0]}_Lv{newLevel}_{parts[2]}";
            
            // IMPORTANT: Update the save system's monster ID list
            var gameManager = ServiceLocator.Get<GameManager>();
            if (gameManager != null)
            {
                // Remove old ID and add new ID to the saved list
                var savedIDs = SaveSystem.LoadSavedMonIDs();
                if (savedIDs.Contains(oldID))
                {
                    savedIDs.Remove(oldID);
                    savedIDs.Add(_controller.monsterID);
                    SaveSystem.SaveMonIDs(savedIDs);

                    // Also update the GameManager's active list
                    gameManager.RemoveSavedMonsterID(oldID);
                    gameManager.AddSavedMonsterID(_controller.monsterID);
                }
            }
            
            // Delete the old save data
            SaveSystem.DeleteMon(oldID);
        }
        else
        {
            Debug.LogWarning($"[Evolution] Could not update monster ID format for {_controller.monsterID}");
        }
    }    private void OnEvolutionComplete(int oldLevel, int newLevel)
    {
        ServiceLocator.Get<UIManager>()?.ShowMessage($"{_controller.MonsterData.monsterName} evolved to level {newLevel}!", 3f);

        _lastEvolutionTime = Time.time;

        // Reset progress counters
        var oldFood = _foodConsumed;
        var oldInteractions = _interactionCount;
        _foodConsumed = 0;
        _interactionCount = 0;

        // IMPORTANT: Save the evolved monster data immediately
        _controller.SaveMonData();
    }

    // Manual evolution for testing/admin
    public void ForceEvolution()
    {
        if (CanEvolve)
        {
            TriggerEvolution();
        }
        else
        {
            Debug.LogWarning($"[Evolution] Force evolution failed - CanEvolve is false for {_controller?.monsterID}");
        }
    }

    // Get evolution progress for UI
    public float GetEvolutionProgress()
    {        var nextRequirement = GetNextEvolutionRequirement();
        if (nextRequirement == null)
        {
            return 1f;
        }

        float progress = 0f;
        int conditions = 0;

        if (nextRequirement.minTimeAlive > 0)
        {
            progress += Mathf.Clamp01(_timeSinceCreation / nextRequirement.minTimeAlive);
            conditions++;
        }
        
        if (nextRequirement.minFoodConsumed > 0)
        {
            progress += Mathf.Clamp01((float)_foodConsumed / nextRequirement.minFoodConsumed);
            conditions++;
        }
        
        if (nextRequirement.minInteractions > 0)
        {
            progress += Mathf.Clamp01((float)_interactionCount / nextRequirement.minInteractions);
            conditions++;
        }

        if (nextRequirement.minCurrentHappiness > 0)
        {
            progress += Mathf.Clamp01(_controller.currentHappiness / nextRequirement.minCurrentHappiness);
            conditions++;
        }
        
        if (nextRequirement.minCurrentHunger > 0)
        {
            progress += Mathf.Clamp01(_controller.currentHunger / nextRequirement.minCurrentHunger);
            conditions++;
        }        float finalProgress = conditions > 0 ? progress / conditions : 1f;
        
        return finalProgress;
    }    public void InitializeWithMonsterData()
    {
        if (_evolutionConfig == null)
        {
            InitializeEvolutionRequirements();
        }
    }
}
