// Tab_UI.cs (새 스크립트)
using UnityEngine;
using UnityEngine.UI;

public class Tab_UI : MonoBehaviour
{
    // 인스펙터에서 이 탭이 어떤 종류의 아이템을 필터링할지 설정
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            // 클릭 이벤트에 필터링 함수 연결
            button.onClick.AddListener(OnTabClicked);
        }
    }

    private void OnTabClicked()
    {

    }
}