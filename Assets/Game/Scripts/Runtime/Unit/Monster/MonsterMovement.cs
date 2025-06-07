using UnityEngine;
using Spine.Unity;


public class MonsterMovement
{
    private RectTransform _transform;
    private MonsterStateMachine _stateMachine;
    private GameManager _gameManager;
    private SkeletonGraphic _spineGraphic;
    
    public MonsterMovement(RectTransform transform, MonsterStateMachine stateMachine, GameManager gameManager, SkeletonGraphic spineGraphic)
    {
        _transform = transform;
        _stateMachine = stateMachine;
        _gameManager = gameManager;
        _spineGraphic = spineGraphic;
    }
    
    public void UpdateMovement(ref Vector2 targetPosition, MonsterDataSO data)
    {
        Vector2 pos = _transform.anchoredPosition;
        float currentSpeed = GetCurrentMoveSpeed(data.moveSpd);
        
        _transform.anchoredPosition = Vector2.MoveTowards(pos, targetPosition, currentSpeed * Time.deltaTime);
        HandleStateSpecificBehavior(pos, targetPosition);
    }
    
    private float GetCurrentMoveSpeed(float baseSpeed)
    {
        if (_stateMachine == null) return baseSpeed;
        
        return _stateMachine.CurrentState switch
        {
            MonsterState.Walking => _stateMachine.behaviorConfig.walkSpeed,
            MonsterState.Running => _stateMachine.behaviorConfig.runSpeed,
            MonsterState.Jumping => 0f,
            _ => 0f
        };
    }
    
    private void HandleStateSpecificBehavior(Vector2 pos, Vector2 target)
    {
        if (_spineGraphic == null || _stateMachine == null) return;
        
        var state = _stateMachine.CurrentState;
        UpdateAnimation(state);
        HandleDirectionalFlipping(pos, target);
    }
    
    private void UpdateAnimation(MonsterState state)
    {
        string animationName = GetAnimationForState(state);
        var current = _spineGraphic.AnimationState.GetCurrent(0);
        
        if (current == null || current.Animation.Name != animationName)
        {
            _spineGraphic.AnimationState.SetAnimation(0, animationName, true);
        }
    }
    
    private string GetAnimationForState(MonsterState state)
    {
        return state switch
        {
            MonsterState.Idle => "idle",
            MonsterState.Walking => "walking",
            MonsterState.Running => "running",
            MonsterState.Jumping => "jumping",
            MonsterState.Itching => "itching",
            MonsterState.Eating => "eating",
            _ => "idle"
        };
    }
    
    private void HandleDirectionalFlipping(Vector2 pos, Vector2 target)
    {
        float direction = target.x - pos.x;
        
        if (Mathf.Abs(direction) > 0.1f)
        {
            Transform parentTransform = _spineGraphic.transform.parent;
            Transform targetTransform = parentTransform ?? _spineGraphic.transform;
            
            Vector3 scale = targetTransform.localScale;
            scale.x = direction > 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            targetTransform.localScale = scale;
        }
    }
}

