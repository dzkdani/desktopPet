using UnityEngine;
using System.Collections.Generic;
using Spine.Unity;

public class MonsterStateMachine : MonoBehaviour
{
    [Header("Configuration")]
    public MonsterBehaviorConfigSO behaviorConfig;

    private MonsterState _currentState = MonsterState.Idle;
    private MonsterState _previousState = MonsterState.Idle;
    private float _stateTimer;
    private float _currentStateDuration;
    private MonsterController _controller;
    private SkeletonGraphic _skeletonGraphic;

    public MonsterState CurrentState => _currentState;
    public MonsterState PreviousState => _previousState;
    public event System.Action<MonsterState> OnStateChanged;

    private void Start()
    {
        _controller = GetComponent<MonsterController>();
        _skeletonGraphic = GetComponentInChildren<SkeletonGraphic>();
        ChangeState(MonsterState.Idle);
    }

    private void Update()
    {
        _stateTimer += Time.deltaTime;

        if (_currentState == MonsterState.Eating && _controller.nearestFood == null)
        {
            ChangeState(MonsterState.Idle);
            return;
        }

        if (_stateTimer >= _currentStateDuration)
        {
            SelectNextState();
        }
    }

    private void SelectNextState()
    {
        if (_currentState == MonsterState.Eating)
        {
            MonsterState nextState = Random.Range(0, 2) == 0 ? MonsterState.Idle : MonsterState.Walking;
            ChangeState(nextState);
            return;
        }

        var possibleTransitions = GetValidTransitions();
        
        if (possibleTransitions.Count == 0) 
        {
            ChangeState(MonsterState.Idle);
            return;
        }

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
            if (transition.requiresFood && _controller.nearestFood == null) continue;
            if (_controller.currentHunger < transition.hungerThreshold) continue;
            if (_controller.currentHappiness < transition.happinessThreshold) continue;

            valid.Add(transition);
        }

        return valid;
    }

    private void ChangeState(MonsterState newState)
    {
        _previousState = _currentState;
        _currentState = newState;
        _stateTimer = 0f;
        
        PlayStateAnimation(newState);
        OnStateChanged?.Invoke(_currentState);
        _currentStateDuration = GetStateDuration(newState);
    }

    private void PlayStateAnimation(MonsterState state)
    {
        if (_skeletonGraphic == null) return;
        
        if (_skeletonGraphic.AnimationState == null)
        {
            if (_skeletonGraphic.skeletonDataAsset != null)
            {
                _skeletonGraphic.Initialize(true);
            }
            
            if (_skeletonGraphic.AnimationState == null)
                return;
        }

        string animationName = state switch
        {
            MonsterState.Idle => "idle",
            MonsterState.Walking => "walking",
            MonsterState.Running => "running", 
            MonsterState.Jumping => "jumping",
            MonsterState.Itching => "itching",
            MonsterState.Eating => "eating",
            _ => "idle"
        };

        bool loop = state switch
        {
            MonsterState.Idle or MonsterState.Walking or MonsterState.Running => true,
            _ => false
        };

        try
        {
            _skeletonGraphic.AnimationState.SetAnimation(0, animationName, loop);
        }
        catch
        {
            // Fallback to idle animation
            try
            {
                _skeletonGraphic.AnimationState.SetAnimation(0, "idle", true);
            }
            catch
            {
                Debug.LogWarning("Failed to set animation for state: " + state);
            }
        }
    }

    private float GetStateDuration(MonsterState state)
    {
        return state switch
        {
            // Movement states: 3-5 seconds
            MonsterState.Walking => GetRandomDuration(
                behaviorConfig?.minWalkDuration, behaviorConfig?.maxWalkDuration, 3f, 5f),
            MonsterState.Running => GetRandomDuration(
                behaviorConfig?.minRunDuration, behaviorConfig?.maxRunDuration, 3f, 5f),
            
            // Non-movement states: 1-3 seconds
            MonsterState.Idle => GetRandomDuration(
                behaviorConfig?.minIdleDuration, behaviorConfig?.maxIdleDuration, 1f, 3f),
            MonsterState.Jumping => GetRandomDuration(
                null, null, 1f, 3f),
            MonsterState.Itching => GetRandomDuration(
                null, null, 1f, 3f),
            MonsterState.Eating => GetRandomDuration(
                null, null, 1f, 3f),
            
            _ => 2f
        };
    }

    private float GetRandomDuration(float? configMin, float? configMax, float defaultMin, float defaultMax)
    {
        float min = configMin > 0 ? configMin.Value : defaultMin;
        float max = configMax > 0 ? configMax.Value : defaultMax;
        return Random.Range(min, max);
    }

    public void ForceState(MonsterState newState)
    {
        ChangeState(newState);
        _currentStateDuration = GetStateDuration(newState);
    }
}