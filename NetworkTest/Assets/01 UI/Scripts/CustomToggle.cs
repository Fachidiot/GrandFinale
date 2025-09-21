using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomToggle : MonoBehaviour
{
    [SerializeField] private GameObject onObject;
    [SerializeField] private GameObject offObject;
    [SerializeField] private bool toggle = true;

    public void Toggle()
    {
        toggle = !toggle;

        if (onObject && offObject)
        {
            if (toggle)
            {
                onObject.SetActive(true);
                offObject.SetActive(false);
            }
            else
            {
                onObject.SetActive(false);
                offObject.SetActive(true);
            }
        }
    }
}
