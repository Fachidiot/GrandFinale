using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class CharacterSelect : MonoBehaviour
{
    [SerializeField] private Sprite sprite;
    [SerializeField] private Image image;
    [SerializeField] private string characterName;

    private TMP_Text text;
    void Awake()
    {
        text = GetComponentInChildren<TMP_Text>();

        text.text = characterName;
        image.sprite = sprite;
    }
}
