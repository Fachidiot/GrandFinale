using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager m_Instance;
    public static GameManager Instance { get { return m_Instance; } }

    [Header("Data References")]
    [SerializeField] private GameSettings gameSettings;
    public GameSettings GameSettings { get { return gameSettings; } }
    [SerializeField] private PlanetDatabase planetDatabase;
    public PlanetDatabase PlanetDatabase { get { return planetDatabase; } }

    public static event Action<bool> OnPauseStateChanged;

    private PlayerInputs playerInputs;
    public PlayerInputs PlayerInput { get { return playerInputs; } }

    [SerializeField] private OptionKeyData initialKey;
    public OptionKeyData GetInitialKeys { get { return initialKey; } }

    private bool optionOn;

    private void Awake()
    {
        if (Instance == null)
        {
            m_Instance = this;
            if (gameObject.scene.name != "DontDestroyOnLoad" && transform.parent == null)
                DontDestroyOnLoad(this);
        }
        else
            Destroy(gameObject);

        playerInputs = GetComponent<PlayerInputs>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // When returning to the main menu, destroy any player objects from the previous session.
        if (scene.name == gameSettings.mainmenuScene)
        {
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.DestroyPendingPlayers();
            }
        }
        // Spawns player objects when entering a playable game scene.
        else if (gameSettings.playableScenes.Contains(scene.name) && NetworkManager.Instance != null)
        {
            if (NetworkManager.Instance.Mode == NetworkMode.SinglePlayer)
            {
                if (PlayerManager.Instance != null)
                {
                    PlayerManager.Instance.SpawnInitialPlayer();
                }
                else
                {
                    Debug.LogError("PlayerManager instance not found! Cannot start single player game.");
                }
            }
            else if (NetworkManager.Instance.Mode == NetworkMode.Host)
            {
                // After the game scene loads, the host needs to re-trigger the player spawning process.
                // Broadcasting the room update will cause all clients (and the host itself)
                // to call PlayerManager.UpdatePlayerList, which will now proceed to spawn
                // the player prefabs because the active scene is correct.
                if (ServerRoomManager.Instance != null)
                {
                    ServerRoomManager.Instance.BroadcastRoomUpdate();
                }
                else
                {
                    Debug.LogError("ServerRoomManager instance not found! Cannot spawn players for host.");
                }
            }
        }
    }

    private void Update()
    {
        // if (GameState.Room == gameState || GameState.Game == gameState)
        // {
        //     if (playerInputs.GetEscape())
        //     {
        //         optionOn = !optionOn;
        //         OnPauseStateChanged?.Invoke(optionOn);
        //     }
        // }
    }

    public void SetPause(bool pause)
    {
        Debug.Log("GamePaused");
        OnPauseStateChanged?.Invoke(pause);
    }

    // Button Methods.
    public void StartOffline()
    {
        // SceneManager.LoadScene(gameSettings.spaceroomScene);
    }

    // public void EndGame()
    // {
    //     Time.timeScale = 1;
    //     gameState = GameState.Title;
    //     AudioManager.Instance.PlayLobbyMusic();
    //     SceneManager.LoadScene(titleScene);
    // }

    public void PlayerDeath()
    {
        Debug.Log("Player Dead...");
    }
}