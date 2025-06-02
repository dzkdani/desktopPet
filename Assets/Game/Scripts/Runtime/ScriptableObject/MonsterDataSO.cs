using UnityEngine;
using Spine;
using Spine.Unity;

[CreateAssetMenu(fileName = "NewMonsterData", menuName = "Monster/Monster Data")]
public class MonsterDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string monsterName;              // Display name
    public string id;               // Unique ID (for save/load)
    public int monPrice = 10;      // Price to buy this monster

    [Header("Classification")]
    public MonsterType monType = MonsterType.Common;

    [Header("Stats")]
    public float moveSpd = 100f;       // Move speed
    public float hungerDepleteRate = 0.05f;  // How fast hunger depletes
    public float poopRate = 20f;     // Default: 20 minutes
    public float baseHunger = 50f;     // Add base hunger
    public float baseHappiness = 0f;   // Add base happiness
    
    [Header("Happiness System")] 
    public float pokeHappinessValue = 2f; // Customizable poke happiness increase - changed from 15f to 2f
    public float areaHappinessRate = 0.2f; // Rate of happiness change based on area - changed from 0.1f to 0.2f

    [Header("Poop Behavior")]
    public bool clickToCollectPoop = true;
    
    [Header("Evolution")]
    public bool canEvolve = true;
    public bool isEvolved = false;
    public bool isFinalEvol = false;
    public int evolutionLevel = 0;    [Header("Evolution Requirements")]
    [Tooltip("Required: Each monster must have its own evolution requirements")]
    public EvolutionRequirementsSO evolutionRequirements;

    [Header("Spine Data")]
    public SkeletonDataAsset[] monsterSpine;

    [Header("Images")]
    public Sprite[] monsImgs;           // [0] base, [1+] evolved forms
}
