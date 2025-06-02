using UnityEngine;
using System;
using System.Linq;

[Serializable]
public class MonsterEvolutionSaveData
{
    public string monsterId;
    public float timeSinceCreation;        // ✅ Keep
    public int foodConsumed;               // ✅ Keep
    public int interactionCount;           // ✅ Keep
    
    // ❌ REMOVE these - they don't make sense:
    // public float totalHappinessAccumulated;
    // public float totalHungerSatisfied;
}

public class MonsterEvolutionHandler
{
    private EvolutionRequirementsSO _evolutionConfig;
    
    private MonsterController _controller;
    
    // Evolution tracking
    private float _lastUpdateTime;
    private float _lastEvolutionTime = -1f;
    private float _evolutionCooldown = 3600f; // 1 hour cooldown

    // ✅ ONLY track these accumulated values:
    private float _timeSinceCreation;
    private int _foodConsumed;
    private int _interactionCount;

    public bool CanEvolve => _controller?.MonsterData != null && _controller.MonsterData.canEvolve && !_controller.MonsterData.isFinalEvol;
    public float TimeSinceCreation => _timeSinceCreation;
    public int FoodConsumed => _foodConsumed;
    public int InteractionCount => _interactionCount;

    public MonsterEvolutionHandler(MonsterController controller)
    {
        _controller = controller;
        InitializeEvolutionRequirements();
    }

    private void InitializeEvolutionRequirements()
    {
        // Check if controller and MonsterData are valid
        if (_controller == null)
        {
            Debug.LogError("MonsterEvolutionHandler: Controller is null!");
            return;
        }
        
        if (_controller.MonsterData == null)
        {
            Debug.LogWarning($"MonsterEvolutionHandler: MonsterData is null for controller. Deferring initialization.");
            return; 
        }
        
        // Every monster MUST have evolution requirements
        if (_controller.MonsterData.evolutionRequirements == null)
        {
            Debug.LogError($"Monster '{_controller.MonsterData.monsterName}' is missing evolution requirements! Please assign EvolutionRequirementsSO.");
            return;
        }
        
        _evolutionConfig = _controller.MonsterData.evolutionRequirements;
    }
    
    private EvolutionRequirement[] GetAvailableEvolutions()
    {
        if (_evolutionConfig == null || _evolutionConfig.requirements == null)
            return new EvolutionRequirement[0];
        
        return _evolutionConfig.requirements
            .Where(req => req.targetEvolutionLevel == _controller.evolutionLevel + 1)
            .ToArray();
    }

    public void LoadEvolutionData(float timeSinceCreation, int foodConsumed, int interactionCount)
    {
        _timeSinceCreation = timeSinceCreation;
        _foodConsumed = foodConsumed;
        _interactionCount = interactionCount;
    }

    public void UpdateEvolutionTracking(float deltaTime)
    {
        if (!CanEvolve || _controller?.MonsterData == null) return;
        
        // ✅ Only track time
        _timeSinceCreation += deltaTime;
        
        if (Time.time - _lastUpdateTime >= 5f)
        {
            CheckEvolutionConditions();
            _lastUpdateTime = Time.time;
        }
    }

    public void OnFoodConsumed()
    {
        _foodConsumed++;  // ✅ Track total food eaten
        CheckEvolutionConditions();
    }

    public void OnInteraction()
    {
        _interactionCount++;  // ✅ Track total interactions
        CheckEvolutionConditions();
    }

    private void CheckEvolutionConditions()
    {
        // Check cooldown
        if (Time.time - _lastEvolutionTime < _evolutionCooldown) return;

        if (!CanEvolve) return;

        var nextEvolution = GetNextEvolutionRequirement();
        if (nextEvolution == null) return;

        if (MeetsEvolutionRequirements(nextEvolution))
        {
            TriggerEvolution();
        }
    }

    private EvolutionRequirement GetNextEvolutionRequirement()
    {
        int currentLevel = _controller.evolutionLevel;

        foreach (var requirement in GetAvailableEvolutions())
        {
            if (requirement.targetEvolutionLevel == currentLevel + 1)
            {
                return requirement;
            }
        }

        return null;
    }

    private bool MeetsEvolutionRequirements(EvolutionRequirement requirement)
    {
        // ✅ Check accumulated progress
        if (_timeSinceCreation < requirement.minTimeAlive) return false;
        if (_foodConsumed < requirement.minFoodConsumed) return false;
        if (_interactionCount < requirement.minInteractions) return false;
        
        // ✅ Check current dynamic status
        if (_controller.currentHappiness < requirement.minCurrentHappiness) return false;
        if (_controller.currentHunger < requirement.minCurrentHunger) return false;

        // ❌ REMOVE these nonsensical checks:
        // if (_totalHappinessAccumulated < requirement.minHappinessAccumulated) return false;
        // if (_totalHungerSatisfied < requirement.minHungerSatisfied) return false;

        return requirement.customCondition?.Invoke(_controller) ?? true;
    }

    public void TriggerEvolution()
    {
        if (!CanEvolve) return;

        var oldLevel = _controller.evolutionLevel;
        var newLevel = oldLevel + 1;

        // Start simple evolution effect
        StartSimpleEvolutionEffect(oldLevel, newLevel);
    }

    private void StartSimpleEvolutionEffect(int oldLevel, int newLevel)
    {
        // Apply evolution changes directly
        _controller.evolutionLevel = newLevel;
        _controller.isEvolved = true;
        UpdateMonsterID(newLevel);
        _controller.UpdateVisuals();
        OnEvolutionComplete(oldLevel, newLevel);
    }

    private void UpdateMonsterID(int newLevel)
    {
        var parts = _controller.monsterID.Split('_');
        if (parts.Length >= 3)
        {
            _controller.monsterID = $"{parts[0]}_Lv{newLevel}_{parts[2]}";
        }
    }

    private void OnEvolutionComplete(int oldLevel, int newLevel)
    {
        ServiceLocator.Get<UIManager>()?.ShowMessage($"{_controller.MonsterData.monsterName} evolved to level {newLevel}!", 3f);

        // ✅ Reset only what makes sense to reset
        _foodConsumed = 0;        // Reset food count for next evolution
        _interactionCount = 0;    // Reset interaction count for next evolution
        // _timeSinceCreation continues accumulating (monster gets older)
        
        // ❌ No need to reset happiness/hunger accumulation since we don't track it
    }

    // Manual evolution for testing/admin
    public void ForceEvolution()
    {
        if (CanEvolve)
        {
            TriggerEvolution();
        }
    }

    // Get evolution progress for UI
    public float GetEvolutionProgress()
    {
        var nextRequirement = GetNextEvolutionRequirement();
        if (nextRequirement == null) return 1f;

        float progress = 0f;
        int conditions = 0;

        // ✅ Accumulated conditions
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

        // ✅ Current status conditions
        if (nextRequirement.minCurrentHappiness > 0)
        {
            progress += Mathf.Clamp01(_controller.currentHappiness / nextRequirement.minCurrentHappiness);
            conditions++;
        }
        
        if (nextRequirement.minCurrentHunger > 0)
        {
            progress += Mathf.Clamp01(_controller.currentHunger / nextRequirement.minCurrentHunger);
            conditions++;
        }

        return conditions > 0 ? progress / conditions : 1f;
    }

    public void InitializeWithMonsterData()
    {
        if (_evolutionConfig == null)
        {
            InitializeEvolutionRequirements();
        }
    }
}
