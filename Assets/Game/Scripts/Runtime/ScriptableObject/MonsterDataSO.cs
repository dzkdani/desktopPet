using UnityEngine;
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
    public float foodDetectionRange = 200f;
    public float eatDistance = 5f;      // Distance to eat food
    

    [Header("Happiness Settings")]
    public float pokeCooldownDuration = 10f;
    public float areaHappinessRate = 0.2f;
    public float pokeHappinessValue = 2f;
    public float hungerHappinessThreshold = 30f; // New field - threshold below which hunger affects happiness
    public float hungerHappinessDrainRate = 2f; // New field - how much happiness drains when hungry

    [Header("Poop Behavior")]
    public bool clickToCollectPoop = true;

    [Header("Evolution")]
    public bool canEvolve = true;
    public bool isEvolved = false;
    public bool isFinalEvol = false;
    public int evolutionLevel = 0;
    public int startingEvolutionLevel = 1; // Or 0, depending on your indexing
    [Header("Evolution Requirements")]
    [Tooltip("Required: Each monster must have its own evolution requirements")]
    public EvolutionRequirementsSO evolutionRequirements;

    [Header("Spine Data")]
    public SkeletonDataAsset[] monsterSpine;

    [Header("Images")]
    public Sprite[] monsImgs;           // [0] base, [1+] evolved forms
    [Header("Sound Effects")]

    public AudioClip[] idleSounds;      // Randomly played during idle state
    public AudioClip happySound;       // Played when happiness increases
    public AudioClip eatSound;         // Played when eating
    public AudioClip hurtSound;       // Played when taking damage
    public AudioClip evolveSound;     // Played during evolution
    public AudioClip deathSound;     // Played when monster dies
    public AudioClip interactionSound; // Played when player interacts
}
