using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RelicSynthesisManager : MonoBehaviour
{
    public static RelicSynthesisManager Instance;

    #region Serialized Fields
    [Header("Components")]
    public SynthesisRoulette roulette;
    public SynthesisResultPopup resultPopup;
    public SynthesisMessagePopup messagePopup;
    public GameObject nodePrefab;
    public Button synthesisButton;

    [Header("Settings")]
    public string magicThreadID = "MAT_REL_01";
    #endregion

    #region Private Fields
    private List<SynthesisNode_UI> activeNodes = new List<SynthesisNode_UI>();
    private string currentBaseGrade = "";
    private bool isSynthesizing = false;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        DOTween.Init();

        if (synthesisButton == null)
            synthesisButton = transform.Find("Synthesis_Button")?.GetComponent<Button>();

        if (synthesisButton != null)
            synthesisButton.onClick.AddListener(TrySynthesize);

        if (roulette == null)
            roulette = GetComponentInChildren<SynthesisRoulette>();

        // 팝업 자동 찾기 (꺼져 있어도 부모 통해 찾기)
        if (resultPopup == null && transform.parent != null)
            resultPopup = transform.parent.GetComponentInChildren<SynthesisResultPopup>(true);

        if (messagePopup == null && transform.parent != null)
            messagePopup = transform.parent.GetComponentInChildren<SynthesisMessagePopup>(true);
    }

    void OnDisable()
    {
        ReturnAllItemsToInventory();
        if (resultPopup != null) resultPopup.ClosePopup();
        if (messagePopup != null) messagePopup.ClosePopup();
        isSynthesizing = false;
    }
    #endregion

    #region Node Management
    public void SpawnNode(InventoryItem item)
    {
        if (isSynthesizing) return;
        if (activeNodes.Any(node => node.LinkedItem == item)) return;

        // 등급 검증
        if (item.item.itemTypeEnum == ItemType.Artifact)
        {
            if (string.IsNullOrEmpty(currentBaseGrade))
            {
                if (!activeNodes.Any(n => n.LinkedItem.item.itemTypeEnum == ItemType.Artifact))
                    currentBaseGrade = item.item.grade;
            }
            else if (currentBaseGrade != item.item.grade)
            {
                ShowWarning("등급 불일치");
                return;
            }
        }

        // 마법실 선제 확인
        int projectedCount = activeNodes.Count + 1;
        int requiredThread = Mathf.Max(0, projectedCount - 1);

        if (InventoryManager.Instance != null)
        {
            if (!InventoryManager.Instance.HasItem(magicThreadID, requiredThread))
            {
                ShowWarning($"마법실이 부족합니다.\n({requiredThread}개 필요)");
                return;
            }
        }

        InventoryManager.Instance.RemoveItem(item, 1);

        GameObject go = Instantiate(nodePrefab, roulette.transform);
        SynthesisNode_UI nodeUI = go.GetComponent<SynthesisNode_UI>();

        nodeUI.Initialize(this, item);

        activeNodes.Add(nodeUI);
        roulette.RefreshLayout(activeNodes);
    }

    public void RemoveNode(SynthesisNode_UI node)
    {
        if (isSynthesizing) return;

        if (activeNodes.Contains(node))
        {
            InventoryManager.Instance.AddItem(node.LinkedItem.item, 1);

            activeNodes.Remove(node);
            Destroy(node.gameObject);

            if (!activeNodes.Any(n => n.LinkedItem.item.itemTypeEnum == ItemType.Artifact))
                currentBaseGrade = "";

            roulette.RefreshLayout(activeNodes);
        }
    }

    private void ReturnAllItemsToInventory()
    {
        if (isSynthesizing) return;

        for (int i = activeNodes.Count - 1; i >= 0; i--)
        {
            InventoryManager.Instance.AddItem(activeNodes[i].LinkedItem.item, 1);
            Destroy(activeNodes[i].gameObject);
        }
        activeNodes.Clear();
        currentBaseGrade = "";
        if (roulette) roulette.RefreshLayout(activeNodes);
    }
    #endregion

    #region Synthesis Logic
    public void TrySynthesize()
    {
        if (isSynthesizing) return;

        var artifacts = activeNodes.Where(n => n.LinkedItem.item.itemTypeEnum == ItemType.Artifact).ToList();
        var boosters = activeNodes.Where(n => n.LinkedItem.item.itemTypeEnum == ItemType.Etc).Select(n => n.LinkedItem.item).ToList();

        if (artifacts.Count < 2)
        {
            ShowWarning("유물 2개 이상 필요");
            return;
        }

        int threadCost = Mathf.Max(1, activeNodes.Count - 1);
        if (!InventoryManager.Instance.HasItem(magicThreadID, threadCost))
        {
            ShowWarning($"마법실 부족\n({threadCost}개 필요)");
            return;
        }

        // --- 조합 시작 ---
        isSynthesizing = true;

        // 마법실 소모
        InventoryManager.Instance.RemoveItemByID(magicThreadID, threadCost);

        if (roulette != null)
        {
            roulette.PlayConvergenceAnimation(() =>
            {
                CompleteSynthesis(artifacts.Count, boosters);
            });
        }
        else
        {
            CompleteSynthesis(artifacts.Count, boosters);
        }
    }

    private void CompleteSynthesis(int artifactCount, List<RelicData> boosters)
    {
        // 1. 팝업 오브젝트가 연결되어 있는지 확인
        if (resultPopup == null)
        {
            Debug.LogError("Result Popup이 연결되지 않았습니다! 인스펙터를 확인하세요.");
            // 연결 안 되어도 인벤토리에라도 넣으려면 아래 로직 진행, 아니면 return
        }
        else
        {
            resultPopup.gameObject.SetActive(true);
        }

        var prob = SynthesisProbabilityTable.GetProbabilities(currentBaseGrade, artifactCount, boosters);
        float roll = Random.Range(0f, 100f);

        int currentTier = SynthesisProbabilityTable.GetTier(currentBaseGrade);
        string resultGrade = currentBaseGrade;

        if (roll < prob.jackpot)
            resultGrade = SynthesisProbabilityTable.GetGradeString(currentTier + 2);
        else if (roll < prob.jackpot + prob.next)
            resultGrade = SynthesisProbabilityTable.GetGradeString(currentTier + 1);

        Debug.Log($"조합 결과 등급: {resultGrade} (확률 롤: {roll})");

        RelicData reward = GetRandomRelicByGrade(resultGrade);

        if (reward != null)
        {
            InventoryManager.Instance.AddItem(reward);

            if (resultPopup != null)
                resultPopup.ShowResult(reward);
            else
                Debug.Log($"[성공] 팝업 없음, 인벤토리 추가됨: {reward.itemName}");
        }
        else
        {
            string errorMsg = $"[{resultGrade}] 등급에 해당하는 Artifact 타입 유물이 DB에 없습니다.";
            Debug.LogError(errorMsg);
            ShowWarning("해당 등급의 유물 데이터가 존재하지 않습니다.");
        }

        // 노드 정리
        foreach (var node in activeNodes) Destroy(node.gameObject);
        activeNodes.Clear();
        currentBaseGrade = "";

        if (roulette) roulette.RefreshLayout(activeNodes);
        isSynthesizing = false;
    }

    private RelicData GetRandomRelicByGrade(string grade)
    {
        if (DataManager.Instance == null) return null;
        var list = DataManager.Instance.RelicDB.Values
            .Where(r => r.grade == grade && r.itemTypeEnum == ItemType.Artifact).ToList();
        return list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
    }

    private void ShowWarning(string msg)
    {
        if (messagePopup != null)
        {
            messagePopup.gameObject.SetActive(true); // 먼저 켜주고
            messagePopup.ShowMessage(msg);
        }
        else Debug.LogWarning(msg);
    }
    #endregion
}