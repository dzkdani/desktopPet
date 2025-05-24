using UnityEngine;

[CreateAssetMenu(fileName = "CoinSO", menuName = "Scriptable Objects/Coin")]
public class CoinSO : ScriptableObject
{
    public CoinType coinType;
    public float onSpawnRate;
    public float offSpawnRate;
    public Sprite coinImg;
    public int coinValue;

    public int CalculateValue(CoinType type)
    {
        if (type == CoinType.Silver) coinValue = 1;
        if (type == CoinType.Gold) coinValue = 10;
        return coinValue;
    }
}
