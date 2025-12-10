using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager m_Instance;
    public static AudioManager Instance { get { return m_Instance; } }

    [Header("BGM")]
    [SerializeField] private AudioClip[] lobbyMusicClips;
    [SerializeField] private AudioClip[] loadingMusicClips;
    [SerializeField] private AudioClip[] backgroundMusicClips;

    [Header("SFX - UI")]
    public AudioClip uiClickClip;
    public AudioClip uiOpenClip;
    public AudioClip uiCloseClip;
    public AudioClip uiTabSwapClip;
    public AudioClip uiHoverClip;
    public AudioClip uiErrorClip;

    [Header("SFX - Item")]
    public AudioClip itemPickupClip;
    public AudioClip itemEquipClip;
    public AudioClip itemUnequipClip;


    [Header("SFX - NPC")]
    public AudioClip shopBuyClip;
    public AudioClip shopSellClip;
    public AudioClip Shop_Reroll;
    public AudioClip upgradeSuccessClip;
    public AudioClip upgradeFailClip;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    private float m_masterVolume = 1f;
    public float MasterVolume { get { return m_masterVolume; } set { m_masterVolume = value; VolumeUpdate(); } }
    private float m_bgmVolume = 1f;
    public float BGMVolume { get { return m_bgmVolume; } set { m_bgmVolume = value; VolumeUpdate(); } }
    private float m_effectVolume = 1f;
    public float EffectVolume { get { return m_effectVolume; } set { m_effectVolume = value; VolumeUpdate(); } }

    void Awake()
    {
        if (Instance == null)
        {
            m_Instance = this;
            if (gameObject.scene.name != "DontDestroyOnLoad" && transform.parent == null)
                DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        PlayLobbyMusic();
    }

    // 폴더 전용 자동 바인딩
    [ContextMenu("Auto Bind Sounds")]
    public void LoadAllSounds()
    {
        // Assets/Resources/Sound/ 경로에서 파일을 가져옵니다.
        string path = "Sound/";

        uiClickClip = Resources.Load<AudioClip>(path + "UI_Click");
        uiOpenClip = Resources.Load<AudioClip>(path + "UI_Open");
        uiCloseClip = Resources.Load<AudioClip>(path + "UI_Open");
        uiTabSwapClip = Resources.Load<AudioClip>(path + "UI_Tab");
        uiHoverClip = Resources.Load<AudioClip>(path + "UI_Hover");
        uiErrorClip = Resources.Load<AudioClip>(path + "UI_Error");

        itemPickupClip = Resources.Load<AudioClip>(path + "Item_Pickup");
        itemEquipClip = Resources.Load<AudioClip>(path + "Item_Equip");
        itemUnequipClip = Resources.Load<AudioClip>(path + "Item_Unequip");

        shopBuyClip = Resources.Load<AudioClip>(path + "Shop_Buy");
        shopSellClip = Resources.Load<AudioClip>(path + "Shop_Sell");
        Shop_Reroll = Resources.Load<AudioClip>(path + "Shop_Reroll");
        upgradeSuccessClip = Resources.Load<AudioClip>(path + "Upgrade_Success");
        upgradeFailClip = Resources.Load<AudioClip>(path + "Upgrade_Fail");

        // BGM
        AudioClip lobbyBgm = Resources.Load<AudioClip>(path + "BGM_Lobby");
        if (lobbyBgm != null) lobbyMusicClips = new AudioClip[] { lobbyBgm };

        Debug.Log("[AudioManager] Resources/Sound 폴더에서 사운드 연결 완료!");
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, m_effectVolume * m_masterVolume);
        }
    }

    public void PlayClickSound() => PlaySFX(uiClickClip);
    public void PlayTabSound() => PlaySFX(uiTabSwapClip);
    public void PlayErrorSound() => PlaySFX(uiErrorClip);
    public void PlayEquipSound() => PlaySFX(itemEquipClip);
    public void PlayUnequipSound() => PlaySFX(itemUnequipClip);
    public void PlayPickupSound() => PlaySFX(itemPickupClip);

    public void PlayLobbyMusic()
    {
        if (lobbyMusicClips == null || lobbyMusicClips.Length == 0 || bgmSource == null) return;
        int random = Random.Range(0, lobbyMusicClips.Length);
        bgmSource.loop = true;
        bgmSource.clip = lobbyMusicClips[random];
        bgmSource.Play();
    }

    public void PlayLoadingMusic()
    {
        if (loadingMusicClips == null || loadingMusicClips.Length == 0 || bgmSource == null) return;
        int random = Random.Range(0, loadingMusicClips.Length);
        bgmSource.loop = true;
        bgmSource.clip = loadingMusicClips[random];
        bgmSource.Play();
    }

    public void PlayBackgroundMusic()
    {
        if (backgroundMusicClips == null || backgroundMusicClips.Length == 0 || bgmSource == null) return;
        int random = Random.Range(0, backgroundMusicClips.Length);
        bgmSource.loop = true;
        bgmSource.clip = backgroundMusicClips[random];
        bgmSource.Play();
    }

    private void VolumeUpdate()
    {
        if (bgmSource != null) bgmSource.volume = m_masterVolume * m_bgmVolume;
        if (sfxSource != null) sfxSource.volume = m_masterVolume * m_effectVolume;
    }
}