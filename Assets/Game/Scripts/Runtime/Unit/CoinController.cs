using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

[System.Serializable]
public class Coin
{
    public CoinType coinType;
    public float onSpawnRate;
    public float offSpawnRate;
    public bool InGame = true;
    public Sprite coinImg;
}

public class CoinController : MonoBehaviour, IPointerDownHandler
{
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

    public int CalculateValue(CoinType type) => value = (int)type;

    // public float CalculateSpawnRate(bool isInGame) => rate = isInGame ? coin.onSpawnRate : coin.offSpawnRate;

    public void OnPointerDown(PointerEventData eventData)
    {
        ServiceLocator.Get<GameManager>().coinCollected += value;

        SaveSystem.SaveCoin(ServiceLocator.Get<GameManager>().coinCollected);
        SaveSystem.Flush();

        ServiceLocator.Get<UIManager>().UpdateCoinCounter();
        ServiceLocator.Get<GameManager>().DespawnPools(gameObject);
    }
}

