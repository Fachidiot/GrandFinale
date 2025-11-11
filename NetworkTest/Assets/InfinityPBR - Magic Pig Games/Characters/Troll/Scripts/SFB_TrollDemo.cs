using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SFB_TrollDemo : MonoBehaviour {

	public void Locomotion(float newValue)
    {
        GetComponent<Animator>().SetFloat("Locomotion", newValue);
    }

    public List<Toggle> wardrobeButtons = new List<Toggle>();

    public void RandomizeButtons()
    {
        foreach (var toggle in wardrobeButtons)
        {
            if (Random.Range(0,2) == 1)
                toggle.isOn = !toggle.isOn;
        }
    }

    public List<Button> swordButtons = new List<Button>();
    public List<Button> armorButtons = new List<Button>();
    public List<Button> bodyButtons = new List<Button>();

    public void RandomizeTextures()
    {
        swordButtons[Random.Range(0, swordButtons.Count)].onClick.Invoke();
        armorButtons[Random.Range(0, armorButtons.Count)].onClick.Invoke();
        bodyButtons[Random.Range(0, bodyButtons.Count)].onClick.Invoke();
    }

    public void RandomAll()
    {
        RandomizeTextures();
        RandomizeButtons();
    }
}
