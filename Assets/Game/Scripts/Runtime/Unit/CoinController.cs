using UnityEngine;
using UnityEngine.EventSystems;

public class CoinController : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] Coin coin;
    [SerializeField] CoinType type;
    [SerializeField] float rate;
    [SerializeField] int value;

    private void OnEnable() {
        value = CalculateValue(type);
    }

    public int CalculateValue(CoinType type)
    {
        if (type == CoinType.Silver) value = 1;
        if (type == CoinType.Gold) value = 10;
        return value;
    }

    public float CalculateSpawnRate(bool isInGame)
    {
        if (isInGame) rate = coin.onSpawnRate;
        if (!isInGame) rate = coin.offSpawnRate;
        return rate;
    }

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
    public float spawnRate;
    public Sprite coinImg;
    public int coinValue;
    public bool InGame = true;
}

public enum CoinType
{
    Silver,
    Gold
}
