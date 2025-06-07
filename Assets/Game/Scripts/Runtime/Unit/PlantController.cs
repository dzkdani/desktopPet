using UnityEngine;
using UnityEngine.UI;

public class PlantController : MonoBehaviour
{
    [Header("References")]
    public Image growthProgressBar;
    public Image waterLevelBar;
    public SpriteRenderer plantSprite;

    [Header("Sprites")]
    public Sprite seedSprite;
    public Sprite growingSprite;
    public Sprite matureSprite;
    public Sprite witheredSprite;

    public string PlantId { get; private set; }

    public void Initialize(PlantSaveData data)
    {
        PlantId = data.plantId;
        UpdateVisuals(data);
    }

    public void UpdateVisuals(PlantSaveData data)
    {
        // Update progress bars
        growthProgressBar.fillAmount = data.growthProgress;
        waterLevelBar.fillAmount = data.waterLevel;

        // Update sprite based on state
        switch (data.currentState)
        {
            case PlantState.Seed:
                plantSprite.sprite = seedSprite;
                break;
            case PlantState.Growing:
                plantSprite.sprite = growingSprite;
                break;
            case PlantState.Mature:
                plantSprite.sprite = matureSprite;
                break;
            case PlantState.Withered:
                plantSprite.sprite = witheredSprite;
                break;
        }

        // Update colors based on water level
        waterLevelBar.color = Color.Lerp(Color.red, Color.blue, data.waterLevel);
    }

    public void OnWaterButtonClicked()
    {
        ServiceLocator.Get<FarmingManager>().WaterPlant(PlantId);
        if (ServiceLocator.Get<FarmingManager>().TryGetPlant(PlantId, out PlantSaveData newData))
        {
            UpdateVisuals(newData);
        }
    }
}