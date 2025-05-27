using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayAreaAdjuster : MonoBehaviour
{
    [Header("Game Area Settings")]
    public RectTransform gameArea;
    [Range(100f, 1920f)] public float gameAreaWidth = 800f;
    [Range(-960f, 960f)] public float horizontalOffset = 0f;

    [Header("Monster Scaling")]
    [Range(0.5f, 2f)] public float monsterScale = 1f;
    public List<MonsterController> monstersToScale;

    [Header("UI Elements")]
    public CanvasScaler canvasScaler;
    [Range(0.5f, 2f)] public float uiScaleFactor = 1f;

    private Vector2 originalSizeDelta;

    void Start()
    {
        if (gameArea != null)
            originalSizeDelta = gameArea.sizeDelta;
    }

    void Update()
    {
        AdjustGameArea();
        AdjustMonsterSize();
        AdjustUIScaling();
    }

    private void AdjustGameArea()
    {
        if (gameArea == null) return;

        Vector2 size = gameArea.sizeDelta;
        size.x = gameAreaWidth;
        gameArea.sizeDelta = size;

        Vector2 anchored = gameArea.anchoredPosition;
        anchored.x = horizontalOffset;
        gameArea.anchoredPosition = anchored;
    }

    private void AdjustMonsterSize()
    {
        foreach (var mon in monstersToScale)
        {
            if (mon != null)
                mon.transform.localScale = Vector3.one * monsterScale;
        }
    }

    private void AdjustUIScaling()
    {
        if (canvasScaler != null)
            canvasScaler.scaleFactor = uiScaleFactor;
    }

    // Optional: Call this after spawning new monsters
    public void RegisterMonster(MonsterController newMonster)
    {
        if (!monstersToScale.Contains(newMonster))
        {
            monstersToScale.Add(newMonster);
            newMonster.transform.localScale = Vector3.one * monsterScale;
        }
    }
}
