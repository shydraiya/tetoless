using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource effectSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip effectClip;

    [Header("Volume")]
    [SerializeField, Range(0, 10)] private int bgmVolume = 10;
    [SerializeField, Range(0, 10)] private int effectVolume = 10;

    public int BgmVolume => bgmVolume;
    public int EffectVolume => effectVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSources();
        ApplyVolumes();
        PlayBgmOnAwake();
    }

    private void OnValidate()
    {
        ApplyVolumes();
    }

    public void SetBgmVolume(int volume)
    {
        bgmVolume = Mathf.Clamp(volume, 0, 10);
        ApplyVolumes();
    }

    public void SetEffectVolume(int volume)
    {
        effectVolume = Mathf.Clamp(volume, 0, 10);
        ApplyVolumes();
    }

    public void SetBgmVolume(float volume)
    {
        SetBgmVolume(Mathf.RoundToInt(volume));
    }

    public void SetEffectVolume(float volume)
    {
        SetEffectVolume(Mathf.RoundToInt(volume));
    }

    public void PlayBgm(AudioClip clip, bool loop = true)
    {
        if (clip == null || bgmSource == null)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    public void PlayEffect(AudioClip clip)
    {
        if (clip != null && effectSource != null)
        {
            effectSource.PlayOneShot(clip);
        }
    }

    public void PlayEffect()
    {
        PlayEffect(effectClip);
    }

    private void SetupAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        if (effectSource == null)
        {
            effectSource = gameObject.AddComponent<AudioSource>();
        }

        bgmSource.playOnAwake = false;
        effectSource.playOnAwake = false;
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume / 10f;
        }

        if (effectSource != null)
        {
            effectSource.volume = effectVolume / 10f;
        }
    }

    private void PlayBgmOnAwake()
    {
        if (bgmClip != null)
        {
            PlayBgm(bgmClip);
        }
    }
}
