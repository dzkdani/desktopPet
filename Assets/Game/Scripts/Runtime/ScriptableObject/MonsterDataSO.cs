using UnityEngine;

[CreateAssetMenu(fileName = "NewMonsterData", menuName = "Monster/Monster Data")]
public class MonsterDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string monName;              // Display name
    public string monID;               // Unique ID (for save/load)

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

    public bool isEvolved = false; // Is this pet evolved?
    [Tooltip("Sprites: 0 = base, 1+ = evolved versions")]
    public Sprite[] petImgs;           // [0] base, [1+] evolved forms
}
