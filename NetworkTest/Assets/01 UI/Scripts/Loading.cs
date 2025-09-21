using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Loading : MonoBehaviour
{
    [Header("Loading")]
    [SerializeField] private Animation image;
    [SerializeField] private LoadingText loadingText;
    [Header("Loading Complete")]
    [SerializeField] private GameObject complete;
    [SerializeField] private GameObject completeTMP;
    [Header("PrevScene")]
    [SerializeField] private GameObject prevScene;

    private bool loadingDone = false;
    public void Load()
    {
        loadingText.LoadingStart();
        StartCoroutine(LoadingStart());
    }
    private void Update()
    {
        if (Input.anyKey && loadingDone)
            NextScene();
    }

    public void NextScene()
    {
        Time.timeScale = 1;
        GameManager.Instance.StartGame();
        prevScene.SetActive(false);
        AudioManager.Instance.PlayBackgroundMusic();
        gameObject.SetActive(false);

        Init();
    }

    private void Init()
    {
        loadingText.gameObject.SetActive(true);
        image.Play();
        image.gameObject.SetActive(true);

        complete.SetActive(false);
        completeTMP.SetActive(false);
        loadingDone = false;
    }

    IEnumerator LoadingStart()
    {
        AsyncOperation async = SceneManager.LoadSceneAsync(GameManager.Instance.GameScene);
        while (async.isDone == false)
        {   //LoadingBar를 NGUI로 하나 만들고 NGUI의 ProgressBar의 SliderValue 값에 넣어 줍니다.
            // Debug.Log(async.progress);
            Time.timeScale = 0;
            yield return true;
        }

        // yield return new WaitForSeconds(loadCompleteTime);
        loadingText.LoadingDone();
        image.Stop();
        image.gameObject.SetActive(false);

        complete.SetActive(true);
        completeTMP.SetActive(true);
        loadingDone = true;
    }
}
