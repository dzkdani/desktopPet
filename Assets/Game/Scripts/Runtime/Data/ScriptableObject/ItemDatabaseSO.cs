using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Database/ItemDatabase")]
public class ItemDatabaseSO : ScriptableObject
{
    public List<ItemDataSO> allItems;

    public ItemDataSO GetItem(string id)
    {
        return allItems.Find(item => item.itemID == id);
    }
}
