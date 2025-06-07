using UnityEngine;

public class MonsterFoodHandler
{
    private MonsterController _controller;
    private GameManager _gameManager;
    private RectTransform _rectTransform;
    private float _foodDetectionRangeSqr;
    private float _eatDistanceSqr;
    private float _cachedFoodDistanceSqr = float.MaxValue;
    private bool _isEating = false;
    private float _lastEatingTime = 0f;
    private const float EATING_COOLDOWN = 3f;
    
    public FoodController NearestFood { get; private set; }
    public bool IsNearFood { get; private set; }
    public bool IsEating => _isEating;
    
    public MonsterFoodHandler(MonsterController controller, GameManager gameManager, RectTransform rectTransform)
    {
        _controller = controller;
        _gameManager = gameManager;
        _rectTransform = rectTransform;
    }
    
    public void Initialize(MonsterDataSO data)
    {
        _foodDetectionRangeSqr = data.foodDetectionRange * data.foodDetectionRange;
        _eatDistanceSqr = data.eatDistance * data.eatDistance;
    }
    
    public void FindNearestFood()
    {
        if (!_controller.IsLoaded) return;
        
        if (Time.time - _lastEatingTime < EATING_COOLDOWN)
        {
            return;
        }

        NearestFood = null;
        float closestSqr = float.MaxValue;
        Vector2 pos = _rectTransform.anchoredPosition;

        foreach (FoodController food in _gameManager.activeFoods)
        {
            if (food == null) continue;

            RectTransform foodRt = food.GetComponent<RectTransform>();
            Vector2 foodPos = foodRt.anchoredPosition;
            float sqrDist = (foodPos - pos).sqrMagnitude;

            if (sqrDist < _foodDetectionRangeSqr && sqrDist < closestSqr)
            {
                if (food.TryClaim(_controller))
                {
                    closestSqr = sqrDist;
                    NearestFood = food;
                }
            }
        }

        _cachedFoodDistanceSqr = NearestFood != null ? closestSqr : float.MaxValue;
        IsNearFood = _cachedFoodDistanceSqr < _eatDistanceSqr;
    }
    
    public void HandleFoodLogic(ref Vector2 targetPosition)
    {
        if (NearestFood == null) return;

        if (_isEating) return;

        if (_controller.GetComponent<MonsterStateMachine>()?.CurrentState == MonsterState.Eating)
        {
            return;
        }

        if (!NearestFood.TryClaim(_controller))
        {
            NearestFood = null;
            _controller.SetRandomTarget();
            return;
        }

        Vector2 currentPos = _rectTransform.anchoredPosition;
        Vector2 foodPos = NearestFood.GetComponent<RectTransform>().anchoredPosition;
        float currentDistanceSqr = (foodPos - currentPos).sqrMagnitude;
        
        float adjustedEatDistanceSqr = _eatDistanceSqr * 3f;
        bool isCurrentlyNearFood = currentDistanceSqr < adjustedEatDistanceSqr;

        if (isCurrentlyNearFood)
        {
            _isEating = true;
            _controller.TriggerEating();
            _controller.StartCoroutine(ConsumeAfterEating());
        }
        else
        {
            targetPosition = foodPos;
        }
    }
    
    private System.Collections.IEnumerator ConsumeAfterEating()
    {
        float eatingDuration = _controller.GetComponent<MonsterStateMachine>()?.GetCurrentStateDuration() ?? 2f;
        yield return new WaitForSeconds(eatingDuration);
        
        if (NearestFood != null)
        {
            _controller.Feed(NearestFood.nutritionValue);
            ServiceLocator.Get<GameManager>().activeFoods.Remove(NearestFood);
            ServiceLocator.Get<GameManager>().DespawnToPool(NearestFood.gameObject);
            NearestFood = null;
        }
        
        _lastEatingTime = Time.time;
        _isEating = false;
        
        var stateMachine = _controller.GetComponent<MonsterStateMachine>();
        stateMachine?.ForceState(MonsterState.Idle);
        
        _controller.SetRandomTarget();
    }

    public void ForceResetEating()
    {
        if (_isEating)
        {
            _controller.StopAllCoroutines();
        }
        
        _isEating = false;
        NearestFood = null;
        
        var stateMachine = _controller.GetComponent<MonsterStateMachine>();
        if (stateMachine?.CurrentState == MonsterState.Eating)
        {
            stateMachine.ForceState(MonsterState.Idle);
        }
        
        _controller.SetRandomTarget();
    }
}
