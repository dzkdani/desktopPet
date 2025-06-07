using UnityEngine;

public enum CursorType { Default, Monster, Poop }

public class CursorManager : MonoBehaviour
{
    [SerializeField] CursorMap map;
    [SerializeField] Vector2 hotspot;

    void Awake()
    {
        Reset();
        ServiceLocator.Register(this);
    }

    public void Set(CursorType t) => Cursor.SetCursor(map.Get(t), hotspot, CursorMode.Auto);
    public void Reset() => Cursor.SetCursor(map.defaultTex, hotspot, CursorMode.Auto);
    void OnDestroy() => ServiceLocator.Unregister<CursorManager>();
}
