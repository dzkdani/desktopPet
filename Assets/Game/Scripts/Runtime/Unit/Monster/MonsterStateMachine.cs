using UnityEngine;
using System.Collections.Generic;
using Spine.Unity;
using UnityEditorInternal;

public class MonsterStateMachine : MonoBehaviour
{
    [Header("Configuration")]
    public MonsterBehaviorConfigSO behaviorConfig;

    private MonsterState _currentState = MonsterState.Idle;
    private MonsterState _previousState = MonsterState.Idle;
    private float _stateTimer;
    private float _currentStateDuration;
    private const float _defaultEatingStateDuration = 10f;
    private MonsterController _controller;
    private SkeletonGraphic _skeletonGraphic;
    private List<StateTransition> _transitions = new List<StateTransition>();

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

        if (_currentState == MonsterState.Eating && _stateTimer > _defaultEatingStateDuration)
        {
            _controller?.ForceResetEating();
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
        _transitions.Clear();
        
        if (behaviorConfig == null || behaviorConfig.transitions == null)
            return _transitions;

        foreach (var transition in behaviorConfig.transitions)
        {
            if (transition.fromState != _currentState) continue;
            if (transition.requiresFood && _controller.nearestFood == null) continue;
            if (_controller.currentHunger < transition.hungerThreshold) continue;
            if (_controller.currentHappiness < transition.happinessThreshold) continue;

            _transitions.Add(transition);
        }

        return _transitions;
    }

    private void ChangeState(MonsterState newState)
    {
        _previousState = _currentState;
        _currentState = newState;
        _stateTimer = 0f;

        PlayStateAnimation(newState);
        OnStateChanged?.Invoke(_currentState); _currentStateDuration = GetStateDuration(newState);
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

        string animationName = GetAvailableAnimation(state);

        bool loop = state switch
        {
            MonsterState.Idle or MonsterState.Walking or MonsterState.Running or MonsterState.Flying => true,
            _ => false
        };

        try
        {
            _skeletonGraphic.AnimationState.SetAnimation(0, animationName, loop);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to set animation '{animationName}' for state {state}: {e.Message}");
            // Fallback to idle animation
            try
            {
                _skeletonGraphic.AnimationState.SetAnimation(0, "idle", true);
            }
            catch (System.Exception fallbackException)
            {
                Debug.LogError($"Critical: Cannot set idle animation for {gameObject.name}: {fallbackException.Message}");
            }
        }
    }

    private string GetAvailableAnimation(MonsterState state)
    {
        string[] preferredAnimations = state switch
        {
            MonsterState.Idle => new[] { "idle" },
            MonsterState.Walking => new[] { "walking", "walk" },
            MonsterState.Running => new[] { "running", "run", "flying", "fly", "walking", "walk" },
            MonsterState.Flying => new[] { "flying", "fly", "running", "run", "walking", "walk" },
            MonsterState.Jumping => new[] { "jumping", "jump" },
            MonsterState.Itching => new[] { "itching", "itch" },
            MonsterState.Eating => new[] { "eating", "eat" },
            _ => new[] { "idle" }
        };

        // Check if any of the preferred animations exist
        foreach (string animName in preferredAnimations)
        {
            if (HasAnimation(animName))
            {
                return animName;
            }
        }

        // Fallback to idle if nothing else works
        return "idle";
    }

    private bool HasAnimation(string animationName)
    {
        if (_skeletonGraphic == null || _skeletonGraphic.skeletonDataAsset == null)
            return false;
            
        var skeletonData = _skeletonGraphic.skeletonDataAsset.GetSkeletonData(false);
        if (skeletonData == null) return false;
        
        var animation = skeletonData.FindAnimation(animationName);
        return animation != null;
    }

    private float GetStateDuration(MonsterState state)
    {
        return state switch
        {
            // Movement states: Keep random duration 3-5 seconds
            MonsterState.Walking => GetRandomDuration(
                behaviorConfig?.minWalkDuration, behaviorConfig?.maxWalkDuration, 3f, 5f),
            MonsterState.Running => GetRandomDuration(
                behaviorConfig?.minRunDuration, behaviorConfig?.maxRunDuration, 3f, 5f),
            MonsterState.Flying => GetRandomDuration(
                behaviorConfig?.minFlyDuration, behaviorConfig?.maxFlyDuration, 3f, 5f),
            
            // Non-movement states: Use animation duration from Spine
            MonsterState.Idle => GetAnimationDuration("idle"),
            MonsterState.Jumping => GetAnimationDuration(GetAvailableAnimation(MonsterState.Jumping)),
            MonsterState.Itching => GetAnimationDuration(GetAvailableAnimation(MonsterState.Itching)),
            MonsterState.Eating => GetAnimationDuration(GetAvailableAnimation(MonsterState.Eating)),
            
            _ => 1f
        };
    }

    private float GetAnimationDuration(string animationName)
    {
        if (_skeletonGraphic == null || _skeletonGraphic.skeletonDataAsset == null)
            return 1f;
            
        var skeletonData = _skeletonGraphic.skeletonDataAsset.GetSkeletonData(false);
        if (skeletonData == null) return 1f;
        
        var animation = skeletonData.FindAnimation(animationName);
        if (animation == null) return 1f;
        
        return animation.Duration;
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

    public float GetCurrentStateDuration()
    {
        return _currentStateDuration;
    }
}