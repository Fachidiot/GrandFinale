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
        // Spawns the player object only when entering the game scene in single-player mode.
        // In multiplayer, player spawning is handled by NetworkPlayerManager based on lobby events.
        if (scene.name == gameSettings.spaceroomScene && NetworkManager.Instance != null && NetworkManager.Instance.Mode == NetworkMode.SinglePlayer)
        {
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.SpawnInitialPlayer();
            }
            else
            {
                Debug.LogError("PlayerManager instance not found! Cannot spawn single player.");
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
        OnPauseStateChanged?.Invoke(pause);
    }

    // Button Methods.
    public void StartOffline()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SetMode(NetworkMode.SinglePlayer);
        }
        else
        {
            Debug.LogError("NetworkManager instance not found!");
            return;
        }

        SceneManager.LoadScene(gameSettings.spaceroomScene);
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