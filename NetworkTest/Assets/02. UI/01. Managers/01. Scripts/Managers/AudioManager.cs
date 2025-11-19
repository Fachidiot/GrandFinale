using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class AudioManager : MonoBehaviour
{
    private static AudioManager m_Instance;
    public static AudioManager Instance { get { return m_Instance; } }
    [SerializeField] private AudioClip[] lobbyMusicClips;
    [SerializeField] private AudioClip[] loadingMusicClips;
    [SerializeField] private AudioClip[] backgroundMusicClips;

    private float m_masterVolume;
    public float MasterVolume { set { m_masterVolume = value; VolumeUpdate(); } }
    private float m_bgmVolume;
    public float BGMVolume { set { m_bgmVolume = value; VolumeUpdate(); } }
    private float m_effectVolume;
    public float EffectVolume { set { m_effectVolume = value; VolumeUpdate(); } }

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            m_Instance = this;
            DontDestroyOnLoad(this);
        }
        else
            Destroy(gameObject);

        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (lobbyMusicClips.Length < 1)
            return;
        int random = Random.Range(0, lobbyMusicClips.Length);
        audioSource.clip = lobbyMusicClips[random];
        audioSource.Play();
    }

    public void PlayLobbyMusic()
    {
        if (lobbyMusicClips.Length <= 0)
            return;
        int random = Random.Range(0, lobbyMusicClips.Length);
        audioSource.clip = lobbyMusicClips[random];
        audioSource.Play();
    }

    public void PlayLoadingMusic()
    {
        if (loadingMusicClips.Length <= 0)
            return;
        int random = Random.Range(0, loadingMusicClips.Length);
        audioSource.clip = loadingMusicClips[random];
        audioSource.Play();
    }

    public void PlayBackgroundMusic()
    {
        if (backgroundMusicClips.Length <= 0)
            return;
        int random = Random.Range(0, backgroundMusicClips.Length);
        audioSource.clip = backgroundMusicClips[random];
        audioSource.Play();
    }

    private void VolumeUpdate()
    {
        if (!audioSource)
            return;

        audioSource.volume = m_masterVolume * m_bgmVolume;
        audioSource.volume = m_masterVolume * m_effectVolume;
    }
}
