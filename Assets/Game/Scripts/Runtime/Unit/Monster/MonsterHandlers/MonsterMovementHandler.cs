using UnityEngine;
using Spine.Unity;
using Unity.VisualScripting;


public class MonsterMovementHandler
{
    private RectTransform _transform;
    private MonsterStateMachine _stateMachine;
    private MonsterController _controller; // NEW: Add controller reference
    private GameManager _gameManager;
    private SkeletonGraphic _spineGraphic;

    public MonsterMovementHandler(RectTransform transform, MonsterStateMachine stateMachine, MonsterController controller, GameManager gameManager, SkeletonGraphic spineGraphic)
    {
        _transform = transform;
        _stateMachine = stateMachine;
        _controller = controller;
        _gameManager = gameManager;
        _spineGraphic = spineGraphic;
    }

    public void UpdateMovement(ref Vector2 targetPosition, MonsterDataSO data)
    {
        Vector2 pos = _transform.anchoredPosition;
        float currentSpeed = GetCurrentMoveSpeed(data);

        // NEW: Reduce speed in very small areas to prevent jittery movement
        // TODO: Implement bounds checking when boundsHandler is available
        // if (_controller.boundsHandler.IsMovementAreaTooSmall())
        // {
        //     currentSpeed *= 0.3f; // Slow down significantly
        // }

        _transform.anchoredPosition = Vector2.MoveTowards(pos, targetPosition, currentSpeed * Time.deltaTime);
        HandleStateSpecificBehavior(pos, targetPosition);
    }

    private float GetCurrentMoveSpeed(MonsterDataSO data)
    {
        if (_stateMachine == null || data == null) return 0f;

        return _stateMachine.CurrentState switch
        {
            MonsterState.Walking => GetWalkSpeed(data),
            MonsterState.Running => GetRunSpeed(data),
            MonsterState.Flying => GetFlySpeed(data),
            MonsterState.Flapping => GetFlySpeed(data) * 0.5f, // Slower flapping movement
            MonsterState.Jumping => 0f,
            MonsterState.Eating => 0f,
            MonsterState.Idle => IsInAir() ? GetFlySpeed(data) * 0.3f : 0f, // Gentle floating for air idle
            MonsterState.Itching => IsInAir() ? GetFlySpeed(data) * 0.2f : 0f, // Very slow air movement
            _ => 0f
        };
    }

    private float GetWalkSpeed(MonsterDataSO data)
    {
        // Try to get from behavior config first
        if (TryGetBehaviorConfigSpeed("walk", out float configSpeed))
            return configSpeed;

        // Fallback to base speed * modifier
        return data.moveSpd * 0.7f;
    }

    private float GetRunSpeed(MonsterDataSO data)
    {
        if (TryGetBehaviorConfigSpeed("run", out float configSpeed))
            return configSpeed;

        return data.moveSpd * 1.3f;
    }

    private float GetFlySpeed(MonsterDataSO data)
    {
        if (TryGetBehaviorConfigSpeed("fly", out float configSpeed))
            return configSpeed;

        return data.moveSpd * 1.1f;
    }

    private bool TryGetBehaviorConfigSpeed(string speedType, out float speed)
    {
        speed = 0f;

        var data = _controller?.MonsterData;
        if (data?.evolutionBehaviors == null) return false;

        var evolutionBehavior = System.Array.Find(data.evolutionBehaviors,
            config => config.evolutionLevel == _controller.evolutionLevel);

        if (evolutionBehavior?.behaviorConfig != null)
        {
            var config = evolutionBehavior.behaviorConfig;
            speed = speedType switch
            {
                "walk" => config.walkSpeed,
                "run" => config.runSpeed,
                "fly" => config.flySpeed, // Assuming this exists
                _ => 0f
            };
            return speed > 0f;
        }

        return false;
    }

    private void HandleStateSpecificBehavior(Vector2 pos, Vector2 target)
    {
        if (_spineGraphic == null || _stateMachine == null) return;

        MonsterState state = _stateMachine.CurrentState;
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
        // Let MonsterStateMachine handle the logic
        return _stateMachine?.GetAvailableAnimation(state) ?? "idle";
    }

    private void HandleDirectionalFlipping(Vector2 pos, Vector2 target)
    {
        float direction = target.x - pos.x;

        // NEW: Different thresholds and cooldowns based on area size
        if (_controller.GetBoundsHandler()?.IsMovementAreaTooSmall() == true)
        {
            // Small areas: Higher threshold + longer cooldown
            float flipThreshold = 50f; // Much higher threshold
            float flipCooldown = 2f;   // 2 second cooldown

            if (Mathf.Abs(direction) > flipThreshold && Time.time - _lastFlipTime > flipCooldown)
            {
                PerformFlip(direction);
                _lastFlipTime = Time.time;
            }
        }
        else
        {
            // Normal areas: Standard flipping
            float flipThreshold = 0.1f;
            if (Mathf.Abs(direction) > flipThreshold)
            {
                PerformFlip(direction);
            }
        }
    }

    // ADD: Flip cooldown tracking
    private float _lastFlipTime = 0f;

    private void PerformFlip(float direction)
    {
        Transform parentTransform = _spineGraphic.transform.parent;
        Transform targetTransform = parentTransform ?? _spineGraphic.transform;

        Vector3 scale = targetTransform.localScale;
        scale.x = direction > 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        targetTransform.localScale = scale;
    }

    // Add helper method to check if monster is in air
    private bool IsInAir()
    {
        Vector2 currentPos = _transform.anchoredPosition;
        // You can access the bounds handler through the controller if needed
        // Or use a simple Y threshold
        return currentPos.y > -200f; // Adjust threshold based on your game area
    }

}

