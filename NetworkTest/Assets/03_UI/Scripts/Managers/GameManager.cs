using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager m_Instance;
    public static GameManager Instance { get { return m_Instance; } }
    [SerializeField] private string titleScene;
    public string TitleScene { get { return titleScene; } }
    [SerializeField] private string gameScene;
    public string GameScene { get { return gameScene; } }
    [SerializeField] private StageInfo stageInfo;
    [SerializeField] private GameObject playerPrefab;

    public static event Action<bool> OnPauseStateChanged;

    [SerializeField] private GameState gameState;
    public GameState CurrentState { get { return gameState; } }
    [SerializeField] private OptionKeyData initialKey;
    public OptionKeyData GetInitialKeys { get { return initialKey; } }

    private PlayerInputs playerInputs;

    private bool optionOn;

    private void Awake()
    {
        if (Instance == null)
        {
            m_Instance = this;
            DontDestroyOnLoad(this);
        }
        else
            Destroy(gameObject);

        playerInputs = GetComponent<PlayerInputs>();
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

    public void EnterLobby()
    {
        gameState = GameState.Lobby;
        optionOn = false;
        OnPauseStateChanged?.Invoke(optionOn);
    }

    public void EnterRoom()
    {
        gameState = GameState.Room;
        optionOn = false;
        OnPauseStateChanged?.Invoke(optionOn);
    }

    // Button Methods.
    public void StartOffline()
    {
        SceneManager.LoadScene("OfflineScene");
    }

    public void PauseGame(bool _value)
    {
        Time.timeScale = _value ? 0 : 1;
    }

    public void StartGame()
    {
        gameState = GameState.Game;
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

public enum GameState
{
    None,
    Title,
    Lobby,
    Room,
    Game
}