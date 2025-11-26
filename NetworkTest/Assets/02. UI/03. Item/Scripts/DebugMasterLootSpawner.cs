using UnityEngine;
using System.Collections.Generic;

public class DebugMasterLootSpawner : MonoBehaviour
{
    [Header("필수 할당")]
    [Tooltip("프로젝트 뷰에 있는 MasterDatabase 에셋을 넣으세요")]
    public MasterDatabase masterDatabase;

    [Tooltip("GenericLootDrop 프리팹을 넣으세요")]
    public GameObject lootPrefab;

    [Header("배치 설정")]
    public float spacing = 0; // 아이템 간격
    public int itemsPerRow = 10;

    // 인스펙터의 컴포넌트 우클릭 메뉴에 버튼을 추가합니다.
    [ContextMenu("1. 모든 아이템 소환 (Spawn All)")]
    public void SpawnAllRelics()
    {
        if (masterDatabase == null || lootPrefab == null)
        {
            Debug.LogError("MasterDatabase 혹은 LootPrefab이 할당되지 않았습니다!");
            return;
        }

        // 기존에 생성된 아이템이 있다면 깔끔하게 지우고 시작
        ClearAllSpawns();

        List<RelicData> relicList = masterDatabase.allRelics;

        Debug.Log($"[Spawner] MasterDB에서 {relicList.Count}개의 아이템을 불러옵니다...");

        for (int i = 0; i < relicList.Count; i++)
        {
            RelicData data = relicList[i];
            if (data == null) continue;

            // 1. 위치 계산 (격자)
            float x = (i % itemsPerRow) * spacing;
            float z = (i / itemsPerRow) * spacing;
            Vector3 spawnPos = transform.position + new Vector3(x, 0, z);

            // 2. 생성
            GameObject go = Instantiate(lootPrefab, spawnPos, Quaternion.identity, this.transform);
            go.name = $"DEBUG_{data.itemName}"; 

            // 3. 데이터 주입
            ItemPickup pickup = go.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                pickup.itemData = data;
                pickup.addToInventoryInstead = true; 
            }

            // 4. 비주얼(색상 등) 초기화
            LootOrbVisuals visuals = go.GetComponent<LootOrbVisuals>();
            if (visuals != null)
            {
                visuals.Initialize(data.grade);
            }
        }

        Debug.Log("[Spawner] 생성 완료!");
    }

    [ContextMenu("2. 전부 삭제 (Clear All)")]
    public void ClearAllSpawns()
    {
        // 내 자식으로 있는 모든 오브젝트 파괴
        // (역순으로 지워야 안전함)
        int childCount = transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        Debug.Log("[Spawner] 초기화 완료");
    }
}