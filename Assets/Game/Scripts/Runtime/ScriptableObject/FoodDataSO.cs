using UnityEngine;

[CreateAssetMenu(fileName = "NewFoodData", menuName = "Food/Food Data")]
public class FoodDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string foodName;              // Display name

    [Header("Stats")]
    public int price;
    public float nutritionValue = 10f;   // How much hunger this food restores
    [Tooltip("Sprites: 0 = base, 1+ = rotten versions")]
    public Sprite[] foodImgs;           // [0] base, [1+] rotten forms
}
