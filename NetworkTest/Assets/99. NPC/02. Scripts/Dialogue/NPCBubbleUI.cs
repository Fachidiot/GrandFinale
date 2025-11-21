using UnityEngine;
using TMPro;
using System.Collections;

public class NPCBubbleUI : MonoBehaviour
{
    [SerializeField] private GameObject bubblePanel;
    [SerializeField] private TMP_Text bubbleText;
    [SerializeField] private float displayDuration = 5f;

    private Coroutine _hideCoroutine;

    private void Awake()
    {
        if (bubblePanel) bubblePanel.SetActive(false);
    }

    public void ShowMessage(string message)
    {
        if (!bubblePanel || !bubbleText) return;
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);

        bubbleText.text = message;
        bubblePanel.SetActive(true);
        _hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    /// <summary>
    /// 외부(DialogueUI 등)에서 말풍선을 강제로 숨길 때 호출하는 함수입니다.
    /// </summary>
    public void Hide() // ★★★ 바로 이 부분입니다! ★★★
    {
        // 숨김 코루틴이 실행 중이었다면 중지하고 즉시 패널을 비활성화합니다.
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        if (bubblePanel) bubblePanel.SetActive(false);
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        bubblePanel.SetActive(false);
    }

    private void LateUpdate()
    {
        if (bubblePanel.activeSelf && Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                             Camera.main.transform.rotation * Vector3.up);
        }
    }
}