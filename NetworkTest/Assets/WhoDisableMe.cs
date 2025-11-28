using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhoDisableMe : MonoBehaviour
{
    private void OnDisable()
    {
        // 씬이 언로드되거나 게임이 종료될 때 호출되는 것은 제외 (노이즈 방지)
        if (!this.gameObject.scene.isLoaded) return;

        // 로그에 호출 스택 전체를 출력합니다.
        // 로그 타입을 Error로 해서 콘솔에서 눈에 띄게 만듭니다.
        Debug.LogError($"[범인 검거] {gameObject.name}이(가) 비활성화 되었습니다! \n 호출 경로: \n {System.Environment.StackTrace}");
    }
}
