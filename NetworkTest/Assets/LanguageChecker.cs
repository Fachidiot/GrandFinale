using TMPro;
using UnityEngine;

public class LanguageChecker : MonoBehaviour
{
    // 사용자가 텍스트를 입력할 Input Field (TextMeshPro 버전)
    public TMP_InputField inputField;

    // '가' 또는 'A'를 표시할 Text UI (TextMeshPro 버전)
    public TextMeshProUGUI statusText;

    void Start()
    {
        if (inputField == null || statusText == null)
        {
            Debug.LogError("InputField 또는 StatusText가 할당되지 않았습니다!");
            return;
        }

        // InputField의 값이 변경될 때마다 OnInputFieldValueChanged 함수를 호출하도록 등록
        inputField.onValueChanged.AddListener(OnInputFieldValueChanged);

        // 초기 상태는 영어로 설정
        statusText.text = "A";
    }

    // InputField의 값이 변경될 때 호출되는 함수
    private void OnInputFieldValueChanged(string text)
    {
        // 입력된 텍스트가 없다면 영어 상태로 유지
        if (string.IsNullOrEmpty(text))
        {
            statusText.text = "A";
            return;
        }

        // 가장 마지막에 입력된 글자를 가져옴
        char lastChar = text[text.Length - 1];

        // 마지막 글자가 한글인지 영어인지 판별
        if (IsKorean(lastChar))
        {
            statusText.text = "가";
        }
        else
        {
            statusText.text = "A";
        }
    }

    /// <summary>
    /// 주어진 문자가 한글인지 확인합니다.
    /// </summary>
    private bool IsKorean(char ch)
    {
        // 한글 유니코드 범위 (가-힣) 안에 있는지 확인
        return (ch >= '\uAC00' && ch <= '\uD7A3');
    }

    // 스크립트가 파괴될 때 이벤트 리스너를 정리해주는 것이 좋습니다.
    void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(OnInputFieldValueChanged);
        }
    }
}