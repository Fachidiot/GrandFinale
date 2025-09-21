using System;
using System.IO;
using System.Threading;
using System.Globalization;
using UnityEngine;
using System.Collections.Generic;

public class OptionDataManager : MonoBehaviour
{
    // Singleton
    private static OptionDataManager m_Instance;
    public static OptionDataManager Instance { get { return m_Instance; } }

    private string OptionDataFileName = "\\Option.json";
    public OptionData OptionData;
    [SerializeField] private OptionKeyData initialKey;

    private SystemLanguage m_Language;
    private OptionManager m_OptionManager;
    private List<Resolution> resolutions = new List<Resolution>();
    public List<Resolution> Resolutions {get{return resolutions;}}
    // private List<RefreshRate> refreshRates = new List<RefreshRate>();

    private void Awake()
    {
        if (Instance == null)
        {
            m_Instance = this;
            // DontDestroyOnLoad(this);

            m_OptionManager = GetComponent<OptionManager>();
            
            // 유저 모니터 해상도 정보 가져오기
            Resolution[] userResolutions = Screen.resolutions;

            // build시 중복되는 해상도가 만들어지는 오류 <- 주사율때문.
            string prevItem = "";
            for (int i = 0; i < userResolutions.Length; ++i)
            {
                if (prevItem != userResolutions[i].width + "x" + userResolutions[i].height)
                {
                    resolutions.Add(userResolutions[i]);
                    prevItem = userResolutions[i].width + "x" + userResolutions[i].height;
                }
            }

            LoadOptionData();
            SaveOptionData();

            // 언어 확인 후 UI언어들 초기화
            InitLanguage();

            // 옵션 확인 후 옵션 UI 초기화
            m_OptionManager.InitMenuLayouts();

            //품질 설정
            QualitySettings.SetQualityLevel(OptionData.m_GraphicQuality, true);
        }
        else
            Destroy(gameObject);
    }

    private void LoadOptionData()
    {
        string filePath = Application.persistentDataPath + OptionDataFileName;

        if (File.Exists(filePath))
        {
            string FromJsonData = File.ReadAllText(filePath);
            OptionData = JsonUtility.FromJson<OptionData>(FromJsonData);
        }

        // 저장된 게임이 없다면
        else
        {
            ResetOptionData();
        }
    }

    // 옵션 데이터 저장하기
    public void SaveOptionData()
    {
        string ToJsonData = JsonUtility.ToJson(OptionData);
        string filePath = Application.persistentDataPath + OptionDataFileName;

        // 이미 저장된 파일이 있다면 덮어쓰기
        File.WriteAllText(filePath, ToJsonData);
    }

    // 데이터를 초기화(새로 생성 포함)하는경우
    public void ResetOptionData()
    {
        print("새로운 옵션 파일 생성");
        OptionData = null;
        OptionData = new OptionData();

        //새로 생성하는 데이터들은 이곳에 선언하기
        // Screens
        OptionData.m_VSync = false;
        for (int i = 0; i < resolutions.Count; ++i)
        {   // 해상도 초기 설정
            if (Screen.currentResolution.width.Equals(resolutions[i].width) && Screen.currentResolution.height.Equals(resolutions[i].height))
                OptionData.m_ScreenResolution = i;
        }
        OptionData.m_FullScreenMode = FullScreenMode.FullScreenWindow;  // 화면 모드 초기 설정
        OptionData.m_ScreenBrightness = 0.5f;

        // Graphics
        OptionData.m_GraphicQuality = QualitySettings.GetQualityLevel();
            // TODO : 추후 수정 필요
        OptionData.m_ShadowQuality = QualitySettings.GetQualityLevel();
        OptionData.m_AmbientOcclusion = QualitySettings.GetQualityLevel();
        OptionData.m_ReflectionQuality = QualitySettings.GetQualityLevel();

        // Sounds
        OptionData.m_MasterVolume = 1;
        OptionData.m_BGMVolume = 1;
        OptionData.m_EffectVolume = 1;

        // Gameplays
        OptionData.m_ScreenVibration = true;
        OptionData.m_Language = Application.systemLanguage; // 언어 초기 설정

        // Shortcut
        OptionData.m_keyData = initialKey;                  // 단축키 초기 설정
        
        //옵션 데이터 저장
        SaveOptionData();
    }

    private void InitLanguage()
    {
        if (PlayerPrefs.GetInt("Language") != 0)
        {
            m_Language = (SystemLanguage)PlayerPrefs.GetInt("Language");
            return;
        }
        else
        {
            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;

            switch (cultureInfo.TwoLetterISOLanguageName)
            {
                case "en":
                    m_Language = SystemLanguage.English;
                    break;
                case "ko":
                    m_Language = SystemLanguage.Korean;
                    break;
                case "ja":
                    m_Language = SystemLanguage.Japanese;
                    break;
            }
            PlayerPrefs.SetInt("Language", (int)m_Language);
        }
    }
}

[Serializable]
public class OptionData
{
    [Header("Screen")]
    public bool m_VSync;
    public FullScreenMode m_FullScreenMode;
    public float m_ScreenBrightness;
    public int m_ScreenResolution;
    public int m_RefreshRate;
    
    [Header("Graphic")]
    public int m_GraphicQuality;
    public int m_ShadowQuality;
    public int m_AmbientOcclusion;
    public int m_ReflectionQuality;

    [Header("Sound")]
    public float m_MasterVolume;
    public float m_BGMVolume;
    public float m_EffectVolume;

    [Header("Gameplay")]
    public bool m_ScreenVibration;
    public SystemLanguage m_Language;

    [Header("Shortcuts")]
    public OptionKeyData m_keyData;
}

[Serializable]
public class OptionKeyData
{
    [Header("Movement")]
    public KeyCode m_KeyMoveLeft;
    public KeyCode m_KeyMoveRight;
    public KeyCode m_KeyMoveUp;
    public KeyCode m_KeyMoveDown;
    public KeyCode m_KeyJump;
    public KeyCode m_KeySprint;
    public KeyCode m_KeyCrouch;

    [Header("Attack")]
    public KeyCode m_Attack;
    public KeyCode m_Aimed;

    [Header("Interaction")]
    public KeyCode m_KeyInteract;
    public KeyCode m_KeyInventory;
    public KeyCode m_KeyEscape;
}
