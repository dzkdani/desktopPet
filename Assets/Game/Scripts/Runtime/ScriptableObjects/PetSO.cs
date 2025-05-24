using UnityEngine;

[CreateAssetMenu(fileName = "PetSO", menuName = "Scriptable Objects/Pet")]
public class PetSO : ScriptableObject
{
    public string    id;
    public Sprite    sprite;
    public float     moveSpeed             = 100f;
    public float     hungerDepletionRate   = 0.1f;
    public float     hungerThresholdToEat  = 30f;
    public float     poopInterval          = 1200f;
    public float     foodDetectionRange    = 200f;
    public float     eatDistance           = 30f;
    public Color     normalColor           = Color.white;
    public Color     hungryColor           = Color.red;
    public float     colorChangeThreshold  = 50f;
}
