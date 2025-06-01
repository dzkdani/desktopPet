using UnityEngine;
using System.Collections.Generic;

public class MonsterSeparationBehavior
{
    private MonsterController _controller;
    private GameManager _gameManager;
    private RectTransform _rectTransform;
    
    [Header("Separation Settings")]
    public float separationRadius = 100f;
    public float separationForce = 2f;
    public LayerMask monsterLayer = -1;
    
    public MonsterSeparationBehavior(MonsterController controller, GameManager gameManager, RectTransform rectTransform)
    {
        _controller = controller;
        _gameManager = gameManager;
        _rectTransform = rectTransform;
    }
    
    public Vector2 CalculateSeparationForce()
    {
        Vector2 separationVector = Vector2.zero;
        int count = 0;
        Vector2 currentPosition = _rectTransform.anchoredPosition;
        
        // Check all other active monsters
        foreach (var otherMonster in _gameManager.activeMonsters)
        {
            if (otherMonster == _controller || otherMonster == null) continue;
            
            var otherTransform = otherMonster.GetComponent<RectTransform>();
            if (otherTransform == null) continue;
            
            Vector2 otherPosition = otherTransform.anchoredPosition;
            float distance = Vector2.Distance(currentPosition, otherPosition);
            
            // If within separation radius, calculate repulsion
            if (distance > 0 && distance < separationRadius)
            {
                Vector2 diff = currentPosition - otherPosition;
                diff.Normalize();
                
                // Weight by distance (closer = stronger repulsion)
                diff /= distance;
                separationVector += diff;
                count++;
            }
        }
        
        // Average the separation vectors
        if (count > 0)
        {
            separationVector /= count;
            separationVector.Normalize();
            separationVector *= separationForce;
        }
        
        return separationVector;
    }
    
    public Vector2 ApplySeparationToTarget(Vector2 originalTarget)
    {
        Vector2 separation = CalculateSeparationForce();
        return originalTarget + separation;
    }
}