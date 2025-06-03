using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private int initialPoolSize = 5;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip mainMenuBGM;
    [SerializeField] private AudioClip gameplayBGM;
    [SerializeField] private AudioClip[] situationalBGM;

    [Header("UI SFX")]
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip menuOpenSFX;
    [SerializeField] private AudioClip notificationSFX;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private string bgmMixerGroup = "BGM";
    [SerializeField] private string sfxMixerGroup = "SFX";
    [SerializeField] private string uiMixerGroup = "UI";
    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
    private Queue<AudioSource> sfxPool = new Queue<AudioSource>();
    private List<AudioSource> activeSfxSources = new List<AudioSource>();
    private Transform sfxPoolParent;

    private void Awake()
    {
        ServiceLocator.Register(this);
        InitializeSFXPool();
        InitializeSFXDictionary();
        AssignOutputsToMixerGroups();
    }

    private void InitializeSFXPool()
    {
        // Create a parent object to keep the hierarchy clean
        sfxPoolParent = new GameObject("SFX_Pool").transform;
        sfxPoolParent.SetParent(transform);

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewAudioSourceInPool();
        }
    }
    private void AssignOutputsToMixerGroups()
    {
        // Assign main sources to mixer groups
        if (masterMixer != null)
        {
            bgmSource.outputAudioMixerGroup = masterMixer.FindMatchingGroups(bgmMixerGroup)[0];
            uiSource.outputAudioMixerGroup = masterMixer.FindMatchingGroups(uiMixerGroup)[0];
        }
    }

    private AudioSource CreateNewAudioSourceInPool()
    {
        GameObject sourceObj = new GameObject($"SFX_Source_{sfxPool.Count}");
        sourceObj.transform.SetParent(sfxPoolParent);
        AudioSource newSource = sourceObj.AddComponent<AudioSource>();
        newSource.playOnAwake = false;

        // Assign to SFX mixer group if available
        if (masterMixer != null)
        {
            var groups = masterMixer.FindMatchingGroups(sfxMixerGroup);
            if (groups.Length > 0)
            {
                newSource.outputAudioMixerGroup = groups[0];
            }
        }

        sfxPool.Enqueue(newSource);
        return newSource;
    }

    private void InitializeSFXDictionary()
    {
        // Add all your SFX to the dictionary here
        // sfxDictionary.Add("monster_hit", hitSFX);
        // sfxDictionary.Add("monster_eat", eatSFX);
    }

    private void Update()
    {
        // Clean up finished SFX sources
        for (int i = activeSfxSources.Count - 1; i >= 0; i--)
        {
            if (!activeSfxSources[i].isPlaying)
            {
                ReturnAudioSourceToPool(activeSfxSources[i]);
                activeSfxSources.RemoveAt(i);
            }
        }
    }

    private AudioSource GetAvailableAudioSource()
    {
        if (sfxPool.Count == 0)
        {
            // If pool is empty, create a new one
            return CreateNewAudioSourceInPool();
        }
        return sfxPool.Dequeue();
    }

    private void ReturnAudioSourceToPool(AudioSource source)
    {
        source.clip = null;
        sfxPool.Enqueue(source);
    }

    #region BGM Controls
    public void PlayMainMenuBGM()
    {
        PlayBGM(mainMenuBGM);
    }

    public void PlayGameplayBGM()
    {
        PlayBGM(gameplayBGM);
    }

    public void PlaySituationalBGM(int index)
    {
        if (index >= 0 && index < situationalBGM.Length)
        {
            PlayBGM(situationalBGM[index]);
        }
    }

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void PauseBGM()
    {
        bgmSource.Pause();
    }

    public void ResumeBGM()
    {
        bgmSource.UnPause();
    }


    #endregion

    #region Enhanced SFX Controls with Pooling
    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableAudioSource();
        source.clip = clip;
        source.volume = volumeScale;
        source.pitch = pitch;
        source.Play();
        activeSfxSources.Add(source);
    }

    public void PlaySFX(string sfxKey, float volumeScale = 1f, float pitch = 1f)
    {
        if (sfxDictionary.TryGetValue(sfxKey, out AudioClip clip))
        {
            PlaySFX(clip, volumeScale, pitch);
        }
        else
        {
            Debug.LogWarning($"SFX with key '{sfxKey}' not found!");
        }
    }

    public void StopAllSFX()
    {
        foreach (var source in activeSfxSources)
        {
            source.Stop();
            ReturnAudioSourceToPool(source);
        }
        activeSfxSources.Clear();
    }
    #endregion
}