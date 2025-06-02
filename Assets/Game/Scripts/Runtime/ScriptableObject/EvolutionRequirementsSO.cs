using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EvolutionRequirements", menuName = "Monster/Evolution Requirements")]
public class EvolutionRequirementsSO : ScriptableObject
{
    [Header("Evolution Chain")]
    public EvolutionRequirement[] requirements;

    [Header("Evolution Settings")]
    public bool allowBranching = false;
    public float evolutionCooldown = 5f;
}

[Serializable]
public class EvolutionRequirement
{
    [Header("Target Evolution")]
    public int targetEvolutionLevel = 1;
    
    [Header("Time Requirements")]
    public float minTimeAlive = 300f; // 5 minutes
    
    [Header("Current Status Requirements (Dynamic)")]
    [Range(0f, 100f)] public float minCurrentHappiness = 80f;  // Must be 80% happy
    [Range(0f, 100f)] public float minCurrentHunger = 70f;     // Must be 70% fed
    
    [Header("Accumulated Progress Requirements")]
    public int minFoodConsumed = 10;     // Must have eaten 10 foods total
    public int minInteractions = 20;     // Must have been touched 20 times total
    
    [Header("Custom Conditions")]
    public Func<MonsterController, bool> customCondition;
    
    [Header("Evolution Info")]
    public string evolutionName = "Evolution";
    public string description = "Evolution requirements";

    [Header("Reset Behavior")]
    public bool resetHappinessProgress = true;
    public bool resetHungerProgress = true;
    public bool resetFoodProgress = true;
    public bool resetInteractionProgress = true;
    [Range(0f, 1f)] public float progressRetentionPercentage = 0f;
}
