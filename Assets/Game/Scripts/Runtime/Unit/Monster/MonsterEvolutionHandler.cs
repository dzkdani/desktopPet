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
    public int InteractionCount => _interactionCount;

    public MonsterEvolutionHandler(MonsterController controller)
    {
        _controller = controller;
        Debug.Log($"[Evolution] Creating evolution handler for monster: {_controller?.monsterID}");
        InitializeEvolutionRequirements();
    }

    private void InitializeEvolutionRequirements()
    {
        Debug.Log($"[Evolution] Initializing evolution requirements for monster: {_controller?.monsterID}");
        
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
        Debug.Log($"[Evolution] Successfully loaded evolution config for {_controller.MonsterData.monsterName}. Requirements count: {_evolutionConfig.requirements?.Length ?? 0}");
        
        if (_evolutionConfig.requirements != null)
        {
            foreach (var req in _evolutionConfig.requirements)
            {
                Debug.Log($"[Evolution] - Level {req.targetEvolutionLevel}: Time={req.minTimeAlive}s, Food={req.minFoodConsumed}, Interactions={req.minInteractions}, Happiness={req.minCurrentHappiness}%, Hunger={req.minCurrentHunger}%");
            }
        }
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
            
        Debug.Log($"[Evolution] Found {available.Length} available evolutions for level {_controller.evolutionLevel} -> {_controller.evolutionLevel + 1}");
        return available;
    }

    public void LoadEvolutionData(float timeSinceCreation, int foodConsumed, int interactionCount)
    {
        _timeSinceCreation = timeSinceCreation;
        _foodConsumed = foodConsumed;
        _interactionCount = interactionCount;
        
        Debug.Log($"[Evolution] Loaded evolution data for {_controller?.monsterID}: Time={timeSinceCreation:F1}s, Food={foodConsumed}, Interactions={interactionCount}");
    }

    public void UpdateEvolutionTracking(float deltaTime)
    {
        if (!CanEvolve || _controller?.MonsterData == null) return;
        
        _timeSinceCreation += deltaTime;
        
        if (Time.time - _lastUpdateTime >= 5f)
        {
            Debug.Log($"[Evolution] Periodic check for {_controller.monsterID}: Time={_timeSinceCreation:F1}s, Food={_foodConsumed}, Interactions={_interactionCount}, Happiness={_controller.currentHappiness:F1}%, Hunger={_controller.currentHunger:F1}%");
            CheckEvolutionConditions();
            _lastUpdateTime = Time.time;
        }
    }

    public void OnFoodConsumed()
    {
        _foodConsumed++;
        Debug.Log($"[Evolution] Food consumed by {_controller?.monsterID}. Total food: {_foodConsumed}");
        CheckEvolutionConditions();
    }

    public void OnInteraction()
    {
        _interactionCount++;
        Debug.Log($"[Evolution] Interaction with {_controller?.monsterID}. Total interactions: {_interactionCount}");
        CheckEvolutionConditions();
    }

    private void CheckEvolutionConditions()
    {
        Debug.Log($"[Evolution] Checking evolution conditions for {_controller?.monsterID}");
        
        // Check cooldown
        // if (Time.time - _lastEvolutionTime < _evolutionCooldown)
        // {
        //     float remainingCooldown = _evolutionCooldown - (Time.time - _lastEvolutionTime);
        //     Debug.Log($"[Evolution] Evolution on cooldown for {_controller?.monsterID}. Remaining: {remainingCooldown:F1}s");
        //     return;
        // }

        if (!CanEvolve)
        {
            Debug.Log($"[Evolution] Monster {_controller?.monsterID} cannot evolve. CanEvolve={_controller?.MonsterData?.canEvolve}, IsFinal={_controller?.MonsterData?.isFinalEvol}");
            return;
        }

        var nextEvolution = GetNextEvolutionRequirement();
        if (nextEvolution == null)
        {
            Debug.LogWarning($"[Evolution] No next evolution requirement found for {_controller?.monsterID} at level {_controller?.evolutionLevel}");
            return;
        }

        Debug.Log($"[Evolution] Checking requirements for {_controller?.monsterID} evolution to level {nextEvolution.targetEvolutionLevel}");
        
        if (MeetsEvolutionRequirements(nextEvolution))
        {
            Debug.Log($"[Evolution] ✓ All requirements met! Triggering evolution for {_controller?.monsterID}");
            TriggerEvolution();
        }
        else
        {
            Debug.Log($"[Evolution] ✗ Requirements not met for {_controller?.monsterID}");
        }
    }

    private EvolutionRequirement GetNextEvolutionRequirement()
    {
        int currentLevel = _controller.evolutionLevel;
        Debug.Log($"[Evolution] Looking for evolution requirement from level {currentLevel} to {currentLevel + 1}");

        foreach (var requirement in GetAvailableEvolutions())
        {
            if (requirement.targetEvolutionLevel == currentLevel + 1)
            {
                Debug.Log($"[Evolution] Found evolution requirement for level {requirement.targetEvolutionLevel}");
                return requirement;
            }
        }

        Debug.LogWarning($"[Evolution] No evolution requirement found for level {currentLevel + 1}");
        return null;
    }

    private bool MeetsEvolutionRequirements(EvolutionRequirement requirement)
    {
        Debug.Log($"[Evolution] Checking individual requirements for {_controller?.monsterID}:");
        
        bool timeCheck = _timeSinceCreation >= requirement.minTimeAlive;
        Debug.Log($"[Evolution] - Time: {_timeSinceCreation:F1}s >= {requirement.minTimeAlive}s = {timeCheck}");
        if (!timeCheck) return false;
        
        bool foodCheck = _foodConsumed >= requirement.minFoodConsumed;
        Debug.Log($"[Evolution] - Food: {_foodConsumed} >= {requirement.minFoodConsumed} = {foodCheck}");
        if (!foodCheck) return false;
        
        bool interactionCheck = _interactionCount >= requirement.minInteractions;
        Debug.Log($"[Evolution] - Interactions: {_interactionCount} >= {requirement.minInteractions} = {interactionCheck}");
        if (!interactionCheck) return false;
        
        bool happinessCheck = _controller.currentHappiness >= requirement.minCurrentHappiness;
        Debug.Log($"[Evolution] - Happiness: {_controller.currentHappiness:F1}% >= {requirement.minCurrentHappiness}% = {happinessCheck}");
        if (!happinessCheck) return false;
        
        bool hungerCheck = _controller.currentHunger >= requirement.minCurrentHunger;
        Debug.Log($"[Evolution] - Hunger: {_controller.currentHunger:F1}% >= {requirement.minCurrentHunger}% = {hungerCheck}");
        if (!hungerCheck) return false;

        bool customCheck = requirement.customCondition?.Invoke(_controller) ?? true;
        Debug.Log($"[Evolution] - Custom condition: {customCheck}");
        
        Debug.Log($"[Evolution] ✓ All requirements satisfied for {_controller?.monsterID}!");
        return customCheck;
    }

    public void TriggerEvolution()
    {
        if (!CanEvolve)
        {
            Debug.LogWarning($"[Evolution] Cannot trigger evolution for {_controller?.monsterID} - CanEvolve is false");
            return;
        }

        var oldLevel = _controller.evolutionLevel;
        var newLevel = oldLevel + 1;

        Debug.Log($"[Evolution] 🎉 EVOLUTION TRIGGERED! {_controller?.monsterID} evolving from level {oldLevel} to {newLevel}");
        
        // Start simple evolution effect
        StartSimpleEvolutionEffect(oldLevel, newLevel);
    }

    private void StartSimpleEvolutionEffect(int oldLevel, int newLevel)
    {
        Debug.Log($"[Evolution] Starting evolution effect for {_controller?.monsterID}: {oldLevel} -> {newLevel}");
        
        // Apply evolution changes directly
        _controller.evolutionLevel = newLevel;
        UpdateMonsterID(newLevel);
        _controller.UpdateVisuals();
        OnEvolutionComplete(oldLevel, newLevel);
        
        Debug.Log($"[Evolution] Evolution effect completed for {_controller?.monsterID}");
    }

    private void UpdateMonsterID(int newLevel)
    {
        var oldID = _controller.monsterID;
        var parts = _controller.monsterID.Split('_');
        if (parts.Length >= 3)
        {
            // Update the existing ID format
            _controller.monsterID = $"{parts[0]}_Lv{newLevel}_{parts[2]}";
            Debug.Log($"[Evolution] Updated monster ID: {oldID} -> {_controller.monsterID}");
            
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
    }

    private void OnEvolutionComplete(int oldLevel, int newLevel)
    {
        Debug.Log($"[Evolution] Evolution complete for {_controller?.monsterID}! Level {oldLevel} -> {newLevel}");

        ServiceLocator.Get<UIManager>()?.ShowMessage($"{_controller.MonsterData.monsterName} evolved to level {newLevel}!", 3f);

        _lastEvolutionTime = Time.time;

        // Reset progress counters
        var oldFood = _foodConsumed;
        var oldInteractions = _interactionCount;
        _foodConsumed = 0;
        _interactionCount = 0;

        Debug.Log($"[Evolution] Reset progress counters - Food: {oldFood} -> {_foodConsumed}, Interactions: {oldInteractions} -> {_interactionCount}");

        // IMPORTANT: Save the evolved monster data immediately
        _controller.SaveMonData();
        Debug.Log($"[Evolution] Monster data saved for {_controller?.monsterID} after evolution");
    }

    // Manual evolution for testing/admin
    public void ForceEvolution()
    {
        Debug.Log($"[Evolution] Force evolution requested for {_controller?.monsterID}");
        
        if (CanEvolve)
        {
            Debug.Log($"[Evolution] Force evolution proceeding for {_controller?.monsterID}");
            TriggerEvolution();
        }
        else
        {
            Debug.LogWarning($"[Evolution] Force evolution failed - CanEvolve is false for {_controller?.monsterID}");
        }
    }

    // Get evolution progress for UI
    public float GetEvolutionProgress()
    {
        var nextRequirement = GetNextEvolutionRequirement();
        if (nextRequirement == null)
        {
            Debug.Log($"[Evolution] No next requirement found for progress calculation - {_controller?.monsterID}");
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
        }

        float finalProgress = conditions > 0 ? progress / conditions : 1f;
        
        if (Time.frameCount % 300 == 0) // Log every 5 seconds at 60fps
        {
            Debug.Log($"[Evolution] Progress for {_controller?.monsterID}: {finalProgress:P1} ({progress:F2}/{conditions})");
        }
        
        return finalProgress;
    }

    public void InitializeWithMonsterData()
    {
        Debug.Log($"[Evolution] Initializing with monster data for {_controller?.monsterID}");
        
        if (_evolutionConfig == null)
        {
            Debug.Log($"[Evolution] Evolution config is null, reinitializing requirements for {_controller?.monsterID}");
            InitializeEvolutionRequirements();
        }
        else
        {
            Debug.Log($"[Evolution] Evolution config already exists for {_controller?.monsterID}");
        }
    }
}
