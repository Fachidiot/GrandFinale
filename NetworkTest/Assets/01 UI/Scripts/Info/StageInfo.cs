using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageInfo : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text text;
    [SerializeField] private TMP_Text infotext;
    [SerializeField] private Map[] maps;

    public void MapSelect(string mapName)
    {
        foreach (var map in maps)
        {
            if (map.name == mapName)
            {
                text.text = map.name + "구";
                infotext.text = map.mapInfo;
                image.sprite = map.sprite;
                image.color = Color.white;
            }
        }
    }
}


[CreateAssetMenu(fileName = "Map", menuName = "Fachidiot/Map", order = 0)]
class Map : ScriptableObject
{
    public Sprite sprite;
    public string mapInfo;
}
