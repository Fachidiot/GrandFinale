using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager m_Instance;
    public static GameManager Instance {get{return m_Instance;}}
    [SerializeField] private string titleScene;
    public string TitleScene {get{return titleScene;}}
    [SerializeField] private string gameScene;
    public string GameScene {get{return gameScene;}}
    [SerializeField] private StageInfo stageInfo;
    [SerializeField] private GameObject playerPrefab;

    [SerializeField] private GameState gameState;
    public GameState CurrentState { get{return gameState;} }
    private void Awake()
    {
        if (Instance == null)
        {
            m_Instance = this;
            DontDestroyOnLoad(this);
        }
        else
            Destroy(gameObject);
    }

    // Button Methods.
    public void ExitGame()
    {
        Application.Quit();
    }

    public void PauseGame(bool _value)
    {
        Time.timeScale = _value ? 0 : 1;
    }

    public void StartGame()
    {
        gameState = GameState.Game;
    }

    public void EndGame()
    {
        Time.timeScale = 1; 
        gameState = GameState.Title;
        AudioManager.Instance.PlayLobbyMusic();
        SceneManager.LoadScene(titleScene);
    }

    public void PlayerDeath()
    {
        Debug.Log("Player Dead...");
    }
}

public enum GameState
{
    None,
    Title,
    Game
}