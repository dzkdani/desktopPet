using UnityEngine;
using UnityEngine.EventSystems;

public class CoinController : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] CoinType type;
    [SerializeField] float rate;
    [SerializeField] int value;

    public void OnPointerDown(PointerEventData eventData)
    {
        GameManager.Instance.coinCollected += value;
        PlayerPrefs.SetInt("Coin", GameManager.Instance.coinCollected);
        PlayerPrefs.Save();
        UIManager.Instance.UpdateCoinCounter();
        GameManager.Instance.DespawnCoin(gameObject);
    }
}

[System.Serializable]
public class Coin
{
    public CoinType coinType;
    public float onSpawnRate;
    public float offSpawnRate;
    public Sprite coinImg;
    public int coinValue;

    public int CoinValue(CoinType type)
    {
        if (type == CoinType.Silver) coinValue = 1;
        if (type == CoinType.Gold) coinValue = 10;
        return coinValue;
    }
}

public enum CoinType
{
    Silver,
    Gold
}
