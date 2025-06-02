using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFacilityData", menuName = "Facility/Facility Data")]
public class FacilityDatabaseSO : ScriptableObject
{
    public List<FacilityDataSO> allFacilities;

    public FacilityDataSO GetFacilityByName(string name)
    {
        return allFacilities.Find(facility => facility.facilityName == name);
    }
}



