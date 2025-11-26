using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RelicSynthesisManager : MonoBehaviour
{
    [Header("Settings")]
    public string magicThreadID = "MAT_REL_01"; // 마법실 ID
    public GameObject nodePrefab;               // 노드 프리팹
    public GameObject linePrefab;               // 선 프리팹
    public Transform synthesisCanvas;           // 노드가 생성될 부모 (Panel)
    public Button synthesisButton;              // 조합 버튼

    // --- 상태 관리 ---
    private List<SynthesisNode_UI> activeNodes = new List<SynthesisNode_UI>();
    private List<GameObject> activeLines = new List<GameObject>();
    private string currentBaseGrade = "";

    void Start()
    {
        if (synthesisButton != null)
            synthesisButton.onClick.AddListener(TrySynthesize);

        if (synthesisCanvas == null) synthesisCanvas = transform;
    }

    void Update()
    {
        // 매 프레임 선을 갱신
        UpdateAutomaticLines();
    }

    // 1. 노드 생성 (드롭 시 호출됨)
    public void SpawnNodeAtPosition(InventoryItem item, Vector3 screenPos)
    {
        if (item.item.itemTypeEnum == ItemType.Artifact)
        {
            // 현재 화면에 유물이 없으면 기준 등급 설정
            if (string.IsNullOrEmpty(currentBaseGrade))
            {
                if (!activeNodes.Any(n => n.LinkedItem.item.itemTypeEnum == ItemType.Artifact))
                    currentBaseGrade = item.item.grade;
            }
            // 기준 등급과 다르면 거부
            else if (currentBaseGrade != item.item.grade)
            {
                Debug.LogWarning("다른 등급의 유물은 섞을 수 없습니다!");
                return;
            }
        }

        // B. 위치 계산
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)synthesisCanvas, screenPos, null, out localPos);

        // C. 노드 생성 및 초기화
        GameObject go = Instantiate(nodePrefab, synthesisCanvas);
        go.transform.localPosition = localPos;

        SynthesisNode_UI nodeUI = go.GetComponent<SynthesisNode_UI>();
        nodeUI.Initialize(this, item);

        activeNodes.Add(nodeUI);
    }

    // 노드 삭제 요청 처리
    public void RemoveNode(SynthesisNode_UI node)
    {
        if (activeNodes.Contains(node))
        {
            activeNodes.Remove(node);
            Destroy(node.gameObject);

            // 남은 유물이 없으면 기준 등급 초기화
            if (!activeNodes.Any(n => n.LinkedItem.item.itemTypeEnum == ItemType.Artifact))
                currentBaseGrade = "";
        }
    }

  
    // 2. 자동 연결 로직
    private void UpdateAutomaticLines()
    {
        // 기존 선 모두 삭제
        foreach (var line in activeLines) Destroy(line);
        activeLines.Clear();

        if (activeNodes.Count < 2) return;

        // 마법실 확인
        if (InventoryManager.Instance != null && !InventoryManager.Instance.HasItem(magicThreadID, 1))
            return;

        // 순차적으로 연결
        for (int i = 0; i < activeNodes.Count - 1; i++)
        {
            DrawLine(activeNodes[i].transform.position, activeNodes[i + 1].transform.position);
        }
    }

    private void DrawLine(Vector3 startPos, Vector3 endPos)
    {
        GameObject line = Instantiate(linePrefab, synthesisCanvas);
        line.transform.SetAsFirstSibling();
        activeLines.Add(line);

        RectTransform rt = line.GetComponent<RectTransform>();

        Vector3 mid = (startPos + endPos) / 2;
        float dist = Vector3.Distance(startPos, endPos);
        Vector3 dir = (endPos - startPos).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        rt.position = mid;
        rt.sizeDelta = new Vector2(dist, 5f);
        rt.rotation = Quaternion.Euler(0, 0, angle);
    }

    // 3. 조합 실행
    public void TrySynthesize()
    {
        // 유물과 재료 분류
        var artifacts = activeNodes.Where(n => n.LinkedItem.item.itemTypeEnum == ItemType.Artifact).ToList();
        var boosters = activeNodes.Where(n => n.LinkedItem.item.itemTypeEnum == ItemType.Etc).Select(n => n.LinkedItem.item).ToList();

        // 조건 체크
        if (artifacts.Count < 2) { Debug.Log("유물 2개 이상 필요"); return; }

        int threadCost = Mathf.Max(1, activeNodes.Count - 1); // 연결 수 = 노드 수 - 1
        if (!InventoryManager.Instance.HasItem(magicThreadID, threadCost))
        {
            Debug.Log($"마법실이 {threadCost}개 필요합니다.");
            return;
        }

        // 확률 계산
        var prob = SynthesisProbabilityTable.GetProbabilities(currentBaseGrade, artifacts.Count, boosters);

        // 결과 판정
        float roll = Random.Range(0f, 100f);
        int currentTier = SynthesisProbabilityTable.GetTier(currentBaseGrade);
        string resultGrade = currentBaseGrade;

        if (roll < prob.jackpot)
            resultGrade = SynthesisProbabilityTable.GetGradeString(currentTier + 2);
        else if (roll < prob.jackpot + prob.next)
            resultGrade = SynthesisProbabilityTable.GetGradeString(currentTier + 1);

        // 결과 처리
        foreach (var node in activeNodes)
        {
            InventoryManager.Instance.RemoveItem(node.LinkedItem, 1);
        }
        InventoryManager.Instance.RemoveItemByID(magicThreadID, threadCost);

        // 결과 지급
        RelicData reward = GetRandomRelicByGrade(resultGrade);
        if (reward != null)
        {
            InventoryManager.Instance.AddItem(reward);
            Debug.Log($"[조합 완료] {reward.itemName} ({reward.grade}) 획득!");
        }
        else
        {
            Debug.Log("조합 실패 또는 해당 등급 아이템 데이터 없음.");
        }

        ClearBoard();
    }

    private RelicData GetRandomRelicByGrade(string grade)
    {
        if (DataManager.Instance == null) return null;
        var list = DataManager.Instance.RelicDB.Values
            .Where(r => r.grade == grade && r.itemTypeEnum == ItemType.Artifact).ToList();
        return list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
    }

    private void ClearBoard()
    {
        foreach (var node in activeNodes) Destroy(node.gameObject);
        activeNodes.Clear();
        foreach (var line in activeLines) Destroy(line);
        activeLines.Clear();
        currentBaseGrade = "";
    }
}