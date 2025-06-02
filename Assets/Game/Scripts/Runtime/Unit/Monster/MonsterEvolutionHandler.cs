using UnityEngine;
using System;

[Serializable]
public class MonsterEvolutionSaveData
{
    public string monsterId;
    public float timeSinceCreation;
    public float totalHappinessAccumulated;
    public float totalHungerSatisfied;
    public int foodConsumed;
    public int interactionCount;
}

[Serializable]
public class EvolutionRequirement
{
    [Header("Target Evolution")]
    public int targetEvolutionLevel = 1;
    
    [Header("Time Requirements")]
    public float minTimeAlive = 300f; // 5 minutes
    
    [Header("Happiness Requirements")]
    public float minHappinessAccumulated = 500f;
    public float minCurrentHappiness = 80f;
    
    [Header("Hunger Requirements")]
    public float minHungerSatisfied = 300f;
    public float minCurrentHunger = 70f;
    
    [Header("Activity Requirements")]
    public int minFoodConsumed = 10;
    public int minInteractions = 20;
    
    [Header("Custom Conditions")]
    public Func<MonsterController, bool> customCondition;
    
    [Header("Evolution Info")]
    public string evolutionName = "Evolution";
    public string description = "Evolution requirements";
}

public class MonsterEvolutionHandler
{
    private EvolutionRequirement[] evolutionRequirements;
    
    private MonsterController _controller;
    private float _timeSinceCreation;
    private float _totalHappinessAccumulated;
    private float _totalHungerSatisfied;
    private int _foodConsumed;
    private int _interactionCount;
    
    // Evolution tracking
    private bool _hasEvolvedThisSession;
    private float _lastHappinessCheck;
    private float _lastHungerCheck;
    private float _lastUpdateTime;

    public bool CanEvolve => _controller.MonsterData.canEvolve && !_controller.MonsterData.isFinalEvol;
    public float TimeSinceCreation => _timeSinceCreation;
    public float TotalHappinessAccumulated => _totalHappinessAccumulated;
    public float TotalHungerSatisfied => _totalHungerSatisfied;
    public int FoodConsumed => _foodConsumed;
    public int InteractionCount => _interactionCount;

    public MonsterEvolutionHandler(MonsterController controller)
    {
        _controller = controller;
        _lastUpdateTime = Time.time;
        InitializeEvolutionRequirements();
    }

    private void InitializeEvolutionRequirements()
    {
        evolutionRequirements = new EvolutionRequirement[]
        {
            new EvolutionRequirement
            {
                targetEvolutionLevel = 1,
                minTimeAlive = 300f, // 5 minutes
                minHappinessAccumulated = 500f,
                minHungerSatisfied = 300f,
                minFoodConsumed = 10,
                minInteractions = 20,
                minCurrentHappiness = 80f,
                minCurrentHunger = 70f
            },
            new EvolutionRequirement
            {
                targetEvolutionLevel = 2,
                minTimeAlive = 900f, // 15 minutes
                minHappinessAccumulated = 1500f,
                minHungerSatisfied = 1000f,
                minFoodConsumed = 30,
                minInteractions = 50,
                minCurrentHappiness = 90f,
                minCurrentHunger = 80f
            }
        };
    }

    public void LoadEvolutionData(float timeSinceCreation, float totalHappiness, float totalHunger, int foodConsumed, int interactionCount)
    {
        _timeSinceCreation = timeSinceCreation;
        _totalHappinessAccumulated = totalHappiness;
        _totalHungerSatisfied = totalHunger;
        _foodConsumed = foodConsumed;
        _interactionCount = interactionCount;
        
        _lastHappinessCheck = _controller.currentHappiness;
        _lastHungerCheck = _controller.currentHunger;
    }

    public void UpdateEvolutionTracking(float deltaTime)
    {
        if (!CanEvolve) return;
        
        _timeSinceCreation += deltaTime;
        
        float currentHappiness = _controller.currentHappiness;
        if (currentHappiness > _lastHappinessCheck)
        {
            _totalHappinessAccumulated += (currentHappiness - _lastHappinessCheck);
        }
        _lastHappinessCheck = currentHappiness;
        
        float currentHunger = _controller.currentHunger;
        if (currentHunger > _lastHungerCheck)
        {
            _totalHungerSatisfied += (currentHunger - _lastHungerCheck);
        }
        _lastHungerCheck = currentHunger;
        
        if (Time.time - _lastUpdateTime >= 5f && !_hasEvolvedThisSession)
        {
            CheckEvolutionConditions();
            _lastUpdateTime = Time.time;
        }
    }

    public void OnFoodConsumed()
    {
        _foodConsumed++;
        CheckEvolutionConditions();
    }

    public void OnInteraction()
    {
        _interactionCount++;
        CheckEvolutionConditions();
    }

    private void CheckEvolutionConditions()
    {
        if (!CanEvolve || _hasEvolvedThisSession) return;

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

        foreach (var requirement in evolutionRequirements)
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
        // Check all conditions
        if (_timeSinceCreation < requirement.minTimeAlive) return false;
        if (_totalHappinessAccumulated < requirement.minHappinessAccumulated) return false;
        if (_totalHungerSatisfied < requirement.minHungerSatisfied) return false;
        if (_foodConsumed < requirement.minFoodConsumed) return false;
        if (_interactionCount < requirement.minInteractions) return false;
        if (_controller.currentHappiness < requirement.minCurrentHappiness) return false;
        if (_controller.currentHunger < requirement.minCurrentHunger) return false;

        // Check custom conditions
        return requirement.customCondition?.Invoke(_controller) ?? true;
    }

    public void TriggerEvolution()
    {
        if (!CanEvolve) return;

        var oldLevel = _controller.evolutionLevel;
        var newLevel = oldLevel + 1;

        // Update evolution level
        _controller.evolutionLevel = newLevel;
        _controller.isEvolved = true;

        // Check if this is final evolution
        if (newLevel >= evolutionRequirements.Length ||
            _controller.MonsterData.monsterSpine.Length <= newLevel + 1)
        {
            _controller.isFinalForm = true;
        }

        // Update monster ID to reflect new evolution level
        UpdateMonsterID(newLevel);

        // Apply visual changes
        _controller.UpdateVisuals();

        // Trigger evolution events
        OnEvolutionComplete(oldLevel, newLevel);

        _hasEvolvedThisSession = true;
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
        // Notify UI/Effects
        ServiceLocator.Get<UIManager>()?.ShowMessage($"{_controller.MonsterData.monsterName} evolved to level {newLevel}!", 3f);

        // Reset some tracking values for next evolution
        _totalHappinessAccumulated = 0;
        _totalHungerSatisfied = 0;
        _foodConsumed = 0;
        _interactionCount = 0;
        _timeSinceCreation = 0;
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
        if (nextRequirement == null) return 1f; // Already at max level

        float progress = 0f;
        int conditions = 0;

        // Calculate progress based on multiple factors
        if (nextRequirement.minTimeAlive > 0)
        {
            progress += Mathf.Clamp01(_timeSinceCreation / nextRequirement.minTimeAlive);
            conditions++;
        }

        if (nextRequirement.minHappinessAccumulated > 0)
        {
            progress += Mathf.Clamp01(_totalHappinessAccumulated / nextRequirement.minHappinessAccumulated);
            conditions++;
        }

        if (nextRequirement.minHungerSatisfied > 0)
        {
            progress += Mathf.Clamp01(_totalHungerSatisfied / nextRequirement.minHungerSatisfied);
            conditions++;
        }

        if (nextRequirement.minFoodConsumed > 0)
        {
            progress += Mathf.Clamp01((float)_foodConsumed / nextRequirement.minFoodConsumed);
            conditions++;
        }

        return conditions > 0 ? progress / conditions : 0f;
    }
}
