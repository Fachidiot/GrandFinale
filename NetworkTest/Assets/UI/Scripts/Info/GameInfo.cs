using TMPro;
using UnityEngine;

public class GameInfo : MonoBehaviour
{
    [SerializeField] private TMP_Text worldNameText;
    [SerializeField] private TMP_Text playTimeText;

    public void SetGameInfo(string _worldName, string _playTime)
    {
        worldNameText.text = _worldName;
        playTimeText.text = _playTime;
    }
}
