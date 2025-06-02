using UnityEngine;

public class MonsterMovementBounds
{
    private RectTransform _rectTransform;
    private GameManager _gameManager;
    
    public MonsterMovementBounds(RectTransform rectTransform, GameManager gameManager)
    {
        _rectTransform = rectTransform;
        _gameManager = gameManager;
    }
    
    public Vector2 GetRandomTarget()
    {
        var bounds = CalculateMovementBounds();
        return new Vector2(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            UnityEngine.Random.Range(bounds.min.y, bounds.max.y)
        );
    }
    
    private (Vector2 min, Vector2 max) CalculateMovementBounds()
    {
        var gameAreaRect = _gameManager.gameArea;
        Vector2 size = gameAreaRect.sizeDelta;

        float halfWidth = _rectTransform.rect.width / 2;
        float halfHeight = _rectTransform.rect.height / 2;

        return (
            new Vector2(-size.x / 2 + halfWidth, -size.y / 2 + halfHeight),
            new Vector2(size.x / 2 - halfWidth, size.y / 2 - halfHeight)
        );
    }
}