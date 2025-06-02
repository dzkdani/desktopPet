using UnityEngine;
using UnityEngine.EventSystems;

public class PoopController : MonoBehaviour, IPointerDownHandler
{
    public int poopValue = 1;

    public void OnPointerDown(PointerEventData eventData)
    {
        ServiceLocator.Get<GameManager>().poopCollected += poopValue;
        ServiceLocator.Get<UIManager>().UpdatePoopCounter();
        ServiceLocator.Get<GameManager>().DespawnPools(gameObject);
    }
}