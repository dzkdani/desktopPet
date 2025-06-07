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