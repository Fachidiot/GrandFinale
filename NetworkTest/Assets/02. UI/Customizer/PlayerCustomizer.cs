using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public class PlayerCustomizer : MonoBehaviour
{
    // 플레이어 Prefab은 총 6종류로 나눠져 있어.
    // 네트워크상 local인 플레이어의 프리팹 (isMine:true인 플레이어) : NetworkLocal_M, NetworkLocal_F
    // 네트워크상 client인 플레이어의 프리팹 (isMine:false인 플레이어) : NetworkClient_M, NetworkClient_F
    // 싱글플레이어 프리팹 : Single_M, Single_F
    private static PlayerCustomizer instance;
    public static PlayerCustomizer Instance { get { return instance; } }

    [Header("Host Prefab")]
    [SerializeField] private GameObject NetworkLocal_F;
    [SerializeField] private GameObject NetworkLocal_M;

    [Header("Client Prefab")]
    [SerializeField] private GameObject NetworkClient_F;
    [SerializeField] private GameObject NetworkClient_M;

    [Header("Single Prefab")]
    [SerializeField] private GameObject Single_F;
    [SerializeField] private GameObject Single_M;

    public ModelInfo Test;

    private ModelInfo _localFemaleModel;    // 여성 프리팹 정보
    public ModelInfo LocalFemaleModelInfo => _localFemaleModel;
    private readonly string LOCAL_FEMALE = "LocalFemaleModel";
    private ModelInfo _localMaleModel;      // 남성 프리팹 정보
    public ModelInfo LocalMaleModelInfo => _localMaleModel;
    private readonly string LOCAL_MALE = "LocalMaleModel";

    private bool _isMale = true;    // UI와 연동해서 UI에서 선택할 성별 저장.
    public bool IsMale { get { return _isMale; } set { _isMale = value; } }

    public bool IsLocalPlayerMale { get; private set; } = true; // Default to male

    private readonly string CUSTOMIZER_FOLDER = "CustomizerData";

    private void InitialCheck()
    {
        if (string.IsNullOrEmpty(Application.persistentDataPath))
        {
            Debug.LogError("PlayerCustomizer: Application.persistentDataPath is not accessible. Cannot save/load customizer data.");
        }

        // Check prefabs
        if (NetworkLocal_M == null) Debug.LogError("PlayerCustomizer: NetworkLocal_M prefab not assigned.");
        if (NetworkLocal_F == null) Debug.LogError("PlayerCustomizer: NetworkLocal_F prefab not assigned.");
        if (NetworkClient_M == null) Debug.LogError("PlayerCustomizer: NetworkClient_M prefab not assigned.");
        if (NetworkClient_F == null) Debug.LogError("PlayerCustomizer: NetworkClient_F prefab not assigned.");
        if (Single_M == null) Debug.LogError("PlayerCustomizer: Single_M prefab not assigned.");
        if (Single_F == null) Debug.LogError("PlayerCustomizer: Single_F prefab not assigned.");
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitialCheck();
        _localFemaleModel = LoadModelInfo(LOCAL_FEMALE);
        _localMaleModel = LoadModelInfo(LOCAL_MALE);
    }

    public void SaveModelInfo(ModelInfo info, string fileName)
    {
        // TODO : ModelInfo 저장.
    }

    public ModelInfo LoadModelInfo(string fileName)
    {
        // TODO : 저장된 ModelInfo 로드.
        return null;
    }

    public GameObject GetNetworkPlayerPrefab(bool isLocal, bool isMale)
    {
        if (isLocal) // Network Local Player (Local Prefab)
        {
            return IsMale ? NetworkLocal_M : NetworkLocal_F;
        }
        else // Network Client Player (Client Prefab)
        {
            return isMale ? NetworkClient_M : NetworkClient_F;
        }
    }

    public GameObject GetSinglePlayerPrefab()
    {
        return IsMale ? Single_M : Single_F;
    }

    public ModelInfo GetLocalPlayerInfo()
    {
        // Only Test
        return Test;
        // return IsMale ? _localMaleModel : _localFemaleModel;
    }
}