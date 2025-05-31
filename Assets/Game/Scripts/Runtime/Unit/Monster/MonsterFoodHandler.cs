using UnityEngine;

public class MonsterFoodHandler
{
    private MonsterController _controller;
    private GameManager _gameManager;
    private RectTransform _rectTransform;
    private float _foodDetectionRangeSqr;
    private float _eatDistanceSqr;
    private float _cachedFoodDistanceSqr = float.MaxValue;
    
    public FoodController NearestFood { get; private set; }
    public bool IsNearFood { get; private set; }
    
    public MonsterFoodHandler(MonsterController controller, GameManager gameManager, RectTransform rectTransform)
    {
        _controller = controller;
        _gameManager = gameManager;
        _rectTransform = rectTransform;
    }
    
    public void Initialize(MonsterData data)
    {
        _foodDetectionRangeSqr = data.foodDetectionRange * data.foodDetectionRange;
        _eatDistanceSqr = data.eatDistance * data.eatDistance;
    }
    
    public void FindNearestFood()
    {
        if (!_controller.IsLoaded) return;

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
                closestSqr = sqrDist;
                NearestFood = food;
            }
        }

        _cachedFoodDistanceSqr = NearestFood != null ? closestSqr : float.MaxValue;
        IsNearFood = _cachedFoodDistanceSqr < _eatDistanceSqr;
    }
    
    public void HandleFoodLogic(ref Vector2 targetPosition)
    {
        if (NearestFood == null) return;

        if (IsNearFood)
        {
            _controller.TriggerEating();
            _controller.Feed(NearestFood.nutritionValue);
            ServiceLocator.Get<GameManager>().DespawnPools(NearestFood.gameObject);
            NearestFood = null;
            _controller.SetRandomTarget();
        }
        else
        {
            targetPosition = NearestFood.GetComponent<RectTransform>().anchoredPosition;
        }
    }
}
