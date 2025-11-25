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

    private ModelInfo _localFemaleModel;    // 여성 프리팹 정보
    public ModelInfo LocalFemaleModelInfo => _localFemaleModel;
    private readonly string LOCAL_FEMALE = "/LocalFemaleModel.json";
    private ModelInfo _localMaleModel;      // 남성 프리팹 정보
    public ModelInfo LocalMaleModelInfo => _localMaleModel;
    private readonly string LOCAL_MALE = "/LocalMaleModel.json";

    private bool _isMale = true;    // UI와 연동해서 UI에서 선택할 성별 저장.
    public bool IsMale { get { return _isMale; } set { _isMale = value; } }

    public bool IsLocalPlayerMale { get; private set; } = true; // Default to male

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
        LoadModelInfo();
    }

    public void SaveModelInfo()
    {
        // TODO : Model ModelInfo 저장.
        string ToJsonData = JsonUtility.ToJson(_localMaleModel);
        string filePath = Application.persistentDataPath + LOCAL_MALE;
        // 이미 저장된 파일이 있다면 덮어쓰기
        File.WriteAllText(filePath, ToJsonData);

        // TODO : Female ModelInfo 저장.
        ToJsonData = JsonUtility.ToJson(_localFemaleModel);
        filePath = Application.persistentDataPath + LOCAL_FEMALE;
        // 이미 저장된 파일이 있다면 덮어쓰기
        File.WriteAllText(filePath, ToJsonData);
    }

    public void LoadModelInfo()
    {
        // TODO : 저장된 Male ModelInfo 로드.
        string filePath = Application.persistentDataPath + LOCAL_MALE;

        if (File.Exists(filePath))
        {
            string FromJsonData = File.ReadAllText(filePath);
            var data = JsonUtility.FromJson<ModelInfo>(FromJsonData);
            _localMaleModel = data;
        }
        else
            _localMaleModel = new ModelInfo();

        // TODO : 저장된 Female ModelInfo 로드.
        filePath = Application.persistentDataPath + LOCAL_FEMALE;

        if (File.Exists(filePath))
        {
            string FromJsonData = File.ReadAllText(filePath);
            _localFemaleModel = JsonUtility.FromJson<ModelInfo>(FromJsonData);
        }
        else
            _localFemaleModel = new ModelInfo();
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
        // // Only Test
        // return Test;
        return IsMale ? _localMaleModel : _localFemaleModel;
    }
}