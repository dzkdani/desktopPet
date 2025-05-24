using UnityEngine;
using UnityEngine.EventSystems;

public class PoopController : MonoBehaviour, IPointerDownHandler
{
    public int poopValue = 1;

    public void OnPointerDown(PointerEventData eventData)
    {
        GameManager.Instance.poopCollected += poopValue;
        PlayerPrefs.SetInt("Money", GameManager.Instance.poopCollected);
        PlayerPrefs.Save();
        UIManager.Instance.UpdatePoopCounter();

        GameManager.Instance.DespawnPoop(gameObject);
    }
}