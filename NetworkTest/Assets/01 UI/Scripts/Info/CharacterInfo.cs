using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfo : MonoBehaviour
{
    public static CharacterInfo Instance;

    [SerializeField] Image image;
    [SerializeField] TMP_Text selectName;
    [SerializeField] TMP_Text selectInfo;
    [SerializeField] Character[] characters;

    private string characterName;
    public void CharacterSelect(string _name)
    {
        if (characterName == _name)
            return;

        characterName = _name;
        // image.sprite = sprite;
        selectName.text = _name;

        foreach (Character character in characters)
        {
            if (character.name == _name)
            {
                selectInfo.text = character.info;
                image.sprite = character.sprite;
                break;
            }
        }
    }
}

[CreateAssetMenu(fileName = "Character", menuName = "Fachidiot/Character", order = 0)]
class Character : ScriptableObject
{
    public Sprite sprite;
    public string info;
}
