using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public class CoinController : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] Coin coin;
    [SerializeField] CoinType type;
    [SerializeField] float rate;
    [SerializeField] int value;

    public void Initialize(CoinType coinType)
    {
        type = coinType;
        // rate = CalculateSpawnRate(coin.InGame);
        value = CalculateValue(type);
        Image image = GetComponent<Image>();
        if (coinType == CoinType.Gold)
        {
            image.color = Color.yellow; // Gold color
        }
        else if (coinType == CoinType.Silver)
        {
            image.color = Color.gray; // Silver color
        }
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


