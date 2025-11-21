#region 설명
/*
-------------------------------------------------------------------------------
1) 플레이어가 상호작용(E키 등) 입력을 했을 때, 주변에서 가장 적절한 대상 찾기
2) 대상이 IInteractable이면 CanInteract 확인 → OnInteract 호출
3) (선택) 프롬프트 문자열(GetPrompt)을 받아 UI에 띄우는 훅 제공


핵심 개념
- 범위 탐색: Physics.OverlapSphere로 간단히 구현 (마스크로 필터링)
- 인터페이스 중심: 대상이 어떤 컴포넌트인지 몰라도 IInteractable만 알면 호출 가능
- UI 분리: 실제 UI는 다른 스크립트가 하도록 문자열만 반환/이벤트만 쏘는 게 좋음
===============================================================================
*/

#endregion

using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    [Header("탐색 설정")]
    [SerializeField] private float interactRange = 3f; // 상호작용 탐색 반경
    [SerializeField] private LayerMask interactableMask; // 탐색 레이어 마스크


    /// <summary>
    /// 주어진 위치를 중심으로 가장 가까운 IInteractable을 찾는다.
    /// </summary>
    public IInteractable FindNearest(Vector3 from)
    {
        var cols = Physics.OverlapSphere(from, interactRange, interactableMask);
        IInteractable best = null;
        float bestDist = float.MaxValue;


        foreach (var c in cols)
        {
            var it = c.GetComponentInParent<IInteractable>();
            if (it == null) continue;
            float d = Vector3.Distance(from, c.transform.position);
            if (d < bestDist) { bestDist = d; best = it; }
        }
        return best;
    }


    /// <summary>
    /// interactor(보통 Player) 기준으로 근접 대상에게 상호작용을 시도한다.
    /// 실제 UI 표시는 여기서 하지 말고, 호출자가 프롬프트 문자열을 받아서 처리하는 것을 권장.
    /// </summary>
    public void TryInteract(GameObject interactor)
    {
        if (!interactor) return;
        var target = FindNearest(interactor.transform.position);
        if (target == null) return;


        if (!target.CanInteract(interactor)) return;
        // 필요하다면: var prompt = target.GetPrompt(); // UI에 띄우도록 외부로 전달
        target.OnInteract(interactor);
    }
}