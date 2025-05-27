using UnityEngine;
using Spine;
using Spine.Unity;

[CreateAssetMenu(fileName = "NewMonsterData", menuName = "Monster/Monster Data")]
public class MonsterDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string monName;              // Display name
    public string monID;               // Unique ID (for save/load)
    public int monPrice = 10;      // Price to buy this monster

    [Header("Classification")]
    public MonsterType monType = MonsterType.Common;

    [Header("Stats")]
    public float moveSpd = 100f;       // Move speed
    public float hungerDepleteRate = 0.1f;  // How fast hunger depletes
    public float poopRate = 1200f;     // Default: 20 minutes in seconds
    [Header("Poop Behavior")]
    public bool clickToCollectPoop = true;
    [Header("Evolution")]
    public bool canEvolve = false;
    public bool isEvolved = false; // Is this evolved?
    public bool isFinalEvol = false; // Is this the final form of the ?
    public int evolutionLevel = 0; // Current evolution level
    [Header("Spine Data")]
    public SkeletonDataAsset[] monsterSpine;

    [Header("Images")]
    [Tooltip("Sprites: 0 = base, 1+ = evolved versions")]
    public Sprite[] monsImgs;           // [0] base, [1+] evolved forms

    public bool EvolutionAvailable()
    {
        // Check if the monster can evolve based on its current state
        return canEvolve;
    }

    public Sprite GetCurrentSprite()
    {
        // Return the sprite based on evolution level
        if (evolutionLevel < monsImgs.Length)
        {
            return monsImgs[evolutionLevel];
        } 
        return monsImgs[0]; // Fallback to base sprite if out of bounds
    }
}
