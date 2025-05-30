using UnityEngine;
using System.Collections.Generic;

public class MonsterStateMachine : MonoBehaviour
{
    [Header("Configuration")]
    public MonsterBehaviorConfigSO behaviorConfig;

    private MonsterState _currentState = MonsterState.Idle;
    private MonsterState _previousState = MonsterState.Idle;
    private float _stateTimer;
    private float _currentStateDuration;
    private MonsterController _controller;

    // Cooldown for poke animations
    private float _pokeCooldownTimer = 0f;
    private const float POKE_COOLDOWN_DURATION = 5f; // 5 seconds cooldown after poke

    public MonsterState CurrentState => _currentState;
    public MonsterState PreviousState => _previousState;
    public event System.Action<MonsterState> OnStateChanged;

    private void Start()
    {
        _controller = GetComponent<MonsterController>();
        ChangeState(MonsterState.Idle);
    }

    private void Update()
    {
        _stateTimer += Time.deltaTime;
        
        // Update poke cooldown timer
        if (_pokeCooldownTimer > 0f)
        {
            _pokeCooldownTimer -= Time.deltaTime;
        }

        if (_stateTimer >= _currentStateDuration)
        {
            SelectNextState();
        }
    }

    private void SelectNextState()
    {
        var possibleTransitions = GetValidTransitions();
        
        if (possibleTransitions.Count == 0) return;

        // Weighted random selection
        float totalWeight = 0f;
        foreach (var transition in possibleTransitions)
            totalWeight += transition.probability;

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var transition in possibleTransitions)
        {
            currentWeight += transition.probability;
            if (randomValue <= currentWeight)
            {
                ChangeState(transition.toState);
                break;
            }
        }
    }

    private List<StateTransition> GetValidTransitions()
    {
        var valid = new List<StateTransition>();
        
        if (behaviorConfig == null || behaviorConfig.transitions == null) return valid;

        foreach (var transition in behaviorConfig.transitions)
        {
            if (transition.fromState != _currentState) continue;

            // Prevent re-entry into poke animations while cooldown is active
            if (_pokeCooldownTimer > 0f && 
                (transition.toState == MonsterState.Jumping || transition.toState == MonsterState.Itching))
                continue;

            if (transition.requiresFood && _controller.nearestFood == null) continue;
            if (_controller.currentHunger < transition.hungerThreshold) continue;

            valid.Add(transition);
        }

        return valid;
    }

    private void ChangeState(MonsterState newState)
    {
        _previousState = _currentState;
        _currentState = newState;
        _stateTimer = 0f;
        _currentStateDuration = GetStateDuration(newState);

        OnStateChanged?.Invoke(_currentState);
    }

    private float GetStateDuration(MonsterState state)
    {
        if (behaviorConfig == null)
        {
            return state switch
            {
                MonsterState.Idle => Random.Range(2f, 4f),
                MonsterState.Walking => Random.Range(3f, 5f),
                MonsterState.Running => Random.Range(3f, 5f),
                MonsterState.Jumping => 1f,
                MonsterState.Itching => Random.Range(2f, 4f),
                MonsterState.Eating => Random.Range(2f, 4f),
                _ => 2f
            };
        }

        return state switch
        {
            MonsterState.Idle => Random.Range(
                behaviorConfig.minIdleDuration > 0 ? behaviorConfig.minIdleDuration : 2f,
                behaviorConfig.maxIdleDuration > 0 ? behaviorConfig.maxIdleDuration : 4f),
            
            MonsterState.Walking => Random.Range(
                behaviorConfig.minWalkDuration > 0 ? behaviorConfig.minWalkDuration : 3f,
                behaviorConfig.maxWalkDuration > 0 ? behaviorConfig.maxWalkDuration : 5f),
            
            MonsterState.Running => Random.Range(
                behaviorConfig.minRunDuration > 0 ? behaviorConfig.minRunDuration : 3f,
                behaviorConfig.maxRunDuration > 0 ? behaviorConfig.maxRunDuration : 5f),
            
            MonsterState.Jumping => behaviorConfig.jumpDuration > 0 ? behaviorConfig.jumpDuration : 1f,
            
            MonsterState.Itching => Random.Range(2f, 4f),
            MonsterState.Eating => Random.Range(2f, 4f),
            
            _ => 2f
        };
    }

    public void ForceState(MonsterState newState)
    {
        // If forcing a poke state, start the cooldown timer
        if (newState == MonsterState.Jumping || newState == MonsterState.Itching)
        {
            _pokeCooldownTimer = POKE_COOLDOWN_DURATION;
        }
        
        ChangeState(newState);
    }
}