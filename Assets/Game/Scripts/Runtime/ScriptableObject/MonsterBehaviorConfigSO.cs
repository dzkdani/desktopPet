using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class StateTransition
{
    public MonsterState fromState;
    public MonsterState toState;
    public float probability;
    public float minDuration;
    public float maxDuration;
    public bool requiresFood = false;
    public float hungerThreshold = 0;
    public float happinessThreshold = 0; // Add happiness threshold
} 
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     
[CreateAssetMenu(fileName = "MonsterBehaviorConfig", menuName = "Monster/Behavior Config")]
public class MonsterBehaviorConfigSO : ScriptableObject
{ 
    [Header("State Probabilities")]
    public StateTransition[] transitions;
    
    [Header("State Durations - Default: Idle/Itching 2-4s, Walk/Run 3-5s, Jump 1s")]
    public float minIdleDuration = 2f;
    public float maxIdleDuration = 4f; 
    public float minWalkDuration = 3f;
    public float maxWalkDuration = 5f;
     public float minRunDuration = 3f;
    public float maxRunDuration = 5f;
    public float jumpDuration = 1f;
    
    [Header("Movement Speeds")]
    public float walkSpeed = 100f;
    public float runSpeed = 200f;
    public float jumpHeight = 50f;
}

public enum MonsterState
{
    Idle,
    Walking,
    Running,
    Itching,
    Jumping,
    Eating
}
