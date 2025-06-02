using UnityEngine;

[System.Serializable]
public class MonsterData
{
    [Header("Core Stats")]
    public float hungerDepletionRate;
    public float happinessDepletionRatef;
    public float poopInterval;
    public float moveSpeed;
    public float foodDetectionRange = 200f;
    public float eatDistance = 5f;
    
    [Header("Happiness System")]
    public float pokeHappinessIncrease;
    public float areaHappinessRate;
    
    [Header("Interaction")]
    public float pokeCooldownDuration = 10f;

    [Header("Separation Behavior")]
    public float separationRadius = 100f;
    public float separationForce = 2f;
    public bool enableSeparation = true;
}