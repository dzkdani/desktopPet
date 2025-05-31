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
    public float hungerDepleteRate = 0.1f;  // How fast hunger depletes - changed from 0.05f to 0.1f
    public float baseHunger = 50f;    // Add this field
    public float baseHappiness = 0f;  // Add this field for base happiness
    public float poopRate = 1200f;     // Default: 20 minutes, changed to 1200f for 20 minutes in seconds
    
    [Header("Happiness System")] 
    public float pokeHappinessValue = 2f; // Customizable poke happiness increase - changed from 15f to 2f
    public float areaHappinessRate = 0.2f; // Rate of happiness change based on area - changed from 0.1f to 0.2f

    [Header("Poop Behavior")]
    public bool clickToCollectPoop = true;
    
    [Header("Evolution")]
    public bool canEvolve = true;
    public bool isEvolved = false; // Is this evolved?
    public bool isFinalEvol = false; // Is this the final form of the ?
    public int evolutionLevel = 0; // Current evolution level
    
    [Header("Spine Data")]
    public SkeletonDataAsset[] monsterSpine;

    [Header("Images")]
    public Sprite[] monsImgs;           // [0] base, [1+] evolved forms
}
