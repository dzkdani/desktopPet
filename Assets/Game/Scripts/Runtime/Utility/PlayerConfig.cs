using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerConfig
{
    // Player stats
    public int coins = 100;
    public int poop = 0;

    // Time tracking
    public DateTime lastLoginTime;
    public TimeSpan totalPlayTime;

    // Monster collection
    public List<string> monsterIDs = new List<string>();
    public Dictionary<string, MonsterSaveData> monsters = new Dictionary<string, MonsterSaveData>();

    // Settings
    public float BGMVolume = 1f;
    public float SFXVolume = 1f;
    public float UIVolume = 1f;
    public bool notificationsEnabled = true;
}