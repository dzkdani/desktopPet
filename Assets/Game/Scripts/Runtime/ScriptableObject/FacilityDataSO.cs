using UnityEngine;

[CreateAssetMenu(fileName = "NewFacilityData", menuName = "Facility/Facility Data")]
public class FacilityDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string facilityName;

    [Header("Stats")]
    public int constructionCost;
    public float buildTime;
    public Sprite facilityImage;
}
