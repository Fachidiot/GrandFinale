using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class ItemPickup : MonoBehaviour
{
    [Header("이 아이템의 데이터")]
    [Tooltip("여기에 RelicData")]
    public RelicData itemData;

    [Header("픽업 방식 설정")]
    [Tooltip("체크하면 8칸 인벤토리로, 체크 해제하면 PlayerAbilityManager로 즉시 등록됩니다.")]
    public bool addToInventoryInstead = false;

    [Tooltip("플레이어가 줍기 범위 내에 들어왔을 때 UI를 띄우기 위한 이벤트")]
    public static event Action<bool, ItemPickup> OnPlayerNearbyPickup;
    private bool playerInRange = false;
    private GameObject nearbyPlayer; 

    private PlayerInputs playerInputs;
    private SphereCollider sphereCollider;

    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 2.  플레이어가 들어왔는지 확인
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TryGetComponent<PlayerInputs>(out playerInputs);
            }

            if (playerInputs != null && playerInputs.enabled)
            {
                playerInRange = true;
                nearbyPlayer = other.gameObject;
                OnPlayerNearbyPickup?.Invoke(true, this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.gameObject == nearbyPlayer)
        {
            playerInRange = false;
            nearbyPlayer = null;
            playerInputs = null;
            OnPlayerNearbyPickup?.Invoke(false, this);
        }
    }

    private void Update()
    {
        if (playerInRange && playerInputs != null)
        {
            if (playerInputs.GetInteract())
            {
                TryPickupItem();
            }
        }
    }

    private void TryPickupItem()
    {
        if (itemData == null)
        {
            Debug.LogWarning("ItemPickup에 itemData가 할당되지 않았습니다!", this);
            return;
        }

        bool success = false;
        if (addToInventoryInstead)
        {
            if (InventoryManager.Instance != null)
            {
                success = InventoryManager.Instance.AddItem(itemData);
            }
        }
        else
        {
            PlayerAbilityManager manager = nearbyPlayer.GetComponentInParent<PlayerAbilityManager>();
            if (manager != null)
            {
                manager.AddRelic(itemData.itemID);
                success = true;
            }
            else
            {
                Debug.LogWarning($"플레이어에게 {itemData.itemName}를 획득할 PlayerAbilityManager가 없습니다.", nearbyPlayer);
            }
        }

        // 아이템 획득에 성공했다면, 네트워크에 알림
        if (success)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPickupSound();
            }
            // UI 숨기기
            OnPlayerNearbyPickup?.Invoke(false, this);

            // 네트워크 동기화된 아이템이라면, 파괴 요청을 호스트에게 보냄
            var networkLoot = GetComponent<NetworkLoot>();
            if (networkLoot != null && NetworkManager.Instance != null)
            {
                JObject pickupMsg = new JObject
                {
                    ["type"] = "picked_up_loot",
                    ["lootNetId"] = networkLoot.lootNetId
                };
                NetworkManager.Instance.SendJsonMessage(NetworkManager.Instance.LobbyHostID, pickupMsg);
            }
            else
            {
                // 네트워크 아이템이 아니라면 로컬에서 즉시 파괴
                Destroy(gameObject);
            }
        }
        else
        {
            if (addToInventoryInstead)
            {
                Debug.Log("인벤토리가 꽉 찼습니다!");
            }
        }
    }

    /// <summary>
    /// 씬 뷰에서 이 오브젝트를 선택했을 때 픽업 범위를 그립니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (sphereCollider == null)
        {
            sphereCollider = GetComponent<SphereCollider>();
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + sphereCollider.center, sphereCollider.radius);
    }
}