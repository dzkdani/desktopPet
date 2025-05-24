using UnityEngine;

[CreateAssetMenu(fileName = "FoodSO", menuName = "Scriptable Objects/Food")]
public class FoodSO : ScriptableObject
{
    public string id;
    public Sprite sprite;
    public float  nutritionValue = 25f;
}
