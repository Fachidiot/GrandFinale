using UnityEngine;

/// <summary>
/// 고정형 NPC 두뇌.
/// 가지고 있는 모듈(Shop, Upgrade, Talk)에 따라 행동이 달라짐.
/// </summary>
public class StationaryBrain : NPCBrain, IInteractable
{
    [Header("Modules (자동 감지됨)")]
    [SerializeField] private ShopModule shopModule;
    [SerializeField] private UpgradeModule upgradeModule;
    [SerializeField] private SimpleTalkModule talkModule;

    [Header("Settings")]
    [SerializeField] private string promptText = "상호작용 [E]";

    protected override void Awake()
    {
        base.Awake();
        // 컴포넌트 자동 할당 (인스펙터에서 비워둬도 알아서 찾음)
        if (!shopModule) shopModule = GetComponent<ShopModule>();
        if (!upgradeModule) upgradeModule = GetComponent<UpgradeModule>();
        if (!talkModule) talkModule = GetComponent<SimpleTalkModule>();
    }

    // === IInteractable 구현 ===
    public bool CanInteract(GameObject interactor)
    {
        return true; // 고정형은 언제나 상호작용 가능 (쿨다운 필요 시 추가)
    }

    public string GetPrompt() => promptText;

    public void OnInteract(GameObject interactor)
    {
        // 1. 단순 대화(말풍선)가 있으면 먼저 실행
        if (talkModule != null)
        {
            talkModule.PlayRandomTalk();
        }

        // 2. 기능 실행 (우선순위: 업그레이드 > 상점)
        // 만약 대화만 하는 NPC라면 아래 로직은 실행되지 않음
        if (upgradeModule != null)
        {
            upgradeModule.ToggleUI();
        }
        else if (shopModule != null)
        {
            // ShopModule.cs에 ToggleShop 함수가 있다고 가정
            shopModule.ToggleShop(interactor.transform);
        }
    }
}