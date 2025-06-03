using UnityEngine;

[CreateAssetMenu(menuName = "Config/Cursor Map")]
public class CursorMap : ScriptableObject
{
    public Texture2D defaultTex;
    public Texture2D monsterTex;
    public Texture2D poopTex;

    public Texture2D Get(CursorType t) => t switch
    {
        CursorType.Monster => monsterTex,
        CursorType.Poop   => poopTex,
        _                 => defaultTex
    };
}

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
