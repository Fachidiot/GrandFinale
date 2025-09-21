using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

public class LoadingText : MonoBehaviour
{
    TMP_Text text;
    int defaultLength;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        defaultLength = text.text.Length;
    }

    public void LoadingStart()
    {
        StartCoroutine(OnTyping());
    }

    public void LoadingDone()
    {
        StopCoroutine(OnTyping());
        gameObject.SetActive(false);
    }

    IEnumerator OnTyping()
    {
        StringBuilder str = new StringBuilder();
        str.Append(text.text);
        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            if (str.Length > defaultLength + 3)
                str.Remove(defaultLength, 4);
            else
            {
                str.Append(".");
                text.text = str.ToString();
            }
        }
    }
}
