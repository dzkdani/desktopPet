using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "FoodDatabase", menuName = "Food/Food Database")]
public class FoodDatabaseSO : ScriptableObject
{
    public List<FoodDataSO> allFoods;

    public FoodDataSO GetFoodByName(string name)
    {
        return allFoods.Find(food => food.foodName == name);
    }
}
