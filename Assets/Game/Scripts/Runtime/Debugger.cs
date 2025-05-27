using UnityEngine;
using UnityEngine.UI;

public class Debugger : MonoBehaviour
{
    public Button resetPlayerPrefBtn;

    private void Awake()
    {
        // resetPlayerPrefBtn.onClick.AddListener(ResetPlayePref);
    }

    public void ResetPlayePref()
    {
        PlayerPrefs.DeleteAll();
    }
}
