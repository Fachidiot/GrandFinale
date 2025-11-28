using UnityEngine;

public class StationaryBrain : NPCBrain, INpcInteractable
{
    [Header("Modules")]
    [SerializeField] private ShopModule shopModule;
    [SerializeField] private UpgradeModule upgradeModule;
    [SerializeField] private SimpleTalkModule simpleTalkModule;

    [Header("UI Settings")]
    [SerializeField] private string interactKeyName = "[F]";

    protected override void Awake()
    {
        base.Awake();
        if (!shopModule) shopModule = GetComponent<ShopModule>();
        if (!upgradeModule) upgradeModule = GetComponent<UpgradeModule>();
        if (!simpleTalkModule) simpleTalkModule = GetComponent<SimpleTalkModule>();
    }

    public string GetPrompt()
    {
        switch (kind)
        {
            case NPCKind.Shop: return $"상점 열기 {interactKeyName}";
            case NPCKind.Upgrade: return $"정비소 열기 {interactKeyName}";
            default: return $"대화하기 {interactKeyName}";
        }
    }

    public void OnInteract(GameObject interactor)
    {
        if (simpleTalkModule) simpleTalkModule.PlayRandomTalk();

        switch (kind)
        {
            case NPCKind.Shop:
                shopModule.ToggleShop(interactor.transform);
                break;

            case NPCKind.Upgrade:
                upgradeModule.ToggleUpgrade(interactor);
                break;

            case NPCKind.SimpleTalk:
                break;
        }
    }
}