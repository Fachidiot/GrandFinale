using UnityEngine;
using Steamworks;

public class CustomSteamManager : MonoBehaviour
{
    public static CustomSteamManager Instance { get; private set; }

    public string PlayerName { get; private set; }
    public bool IsSteamInitialized { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            // Steamworks.NET의 SteamAPI.Init()을 호출
            // 이전에 발생했던 DllNotFoundException은 여기서 처리되지 않습니다.
            // 해당 예외는 Steamworks.NET의 SteamManager에서 처리됩니다.
            if (SteamAPI.Init())
            {
                PlayerName = SteamFriends.GetPersonaName();
                IsSteamInitialized = true;
                Debug.Log($"Steam Initialized. Player Name: {PlayerName}");
            }
            else
            {
                Debug.LogError("SteamAPI.Init() failed. Is Steam client running?");
                PlayerName = "Player" + Random.Range(1000, 9999);
                IsSteamInitialized = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SteamAPI.Init() threw an exception: {e.Message}");
            PlayerName = "Player" + Random.Range(1000, 9999);
            IsSteamInitialized = false;
        }
    }

    private void Update()
    {
        if (IsSteamInitialized)
        {
            // Debug.Log("Running Steam Callbacks");
            SteamAPI.RunCallbacks();
        }
    }

    private void OnDestroy()
    {
        if (IsSteamInitialized)
        {
            SteamAPI.Shutdown();
            Debug.Log("Steam Shutdown.");
        }
    }
}
