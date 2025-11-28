using UnityEngine;
using Cinemachine;
using System.Collections;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class DungeonEntryAction : MonoBehaviour
{
    [Header("Settings")]
    // [SerializeField] private string interactPrompt = "Interact [F]";

    [Tooltip("Leave empty if moving within the current scene")]
    [SerializeField] private string targetSceneName;

    [Tooltip("Name of the parent object to arrive at (e.g., Goto_stage2, DungeonEntrance)")]
    [SerializeField] private string targetLocationName;

    [Header("Cutscene (Optional)")]
    [SerializeField] private CinemachineVirtualCamera entryCamera;
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField] private float sequenceDuration = 2.0f;

    private bool isPlayerInZone = false;
    private GameObject cachedPlayer;
    private PlayerInputs cachedPlayerInputs;
    private static string _pendingLocationName;

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Update()
    {
        if (isPlayerInZone && cachedPlayerInputs != null)
        {
            if (cachedPlayerInputs.GetInteract())
            {
                Debug.Log($"[DungeonEntry] Interaction detected from {cachedPlayer.name}");
                StartCoroutine(ProcessEntryRoutine(cachedPlayer));
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || LayerMask.LayerToName(other.gameObject.layer) == "Player")
        {
            cachedPlayerInputs = other.GetComponent<PlayerInputs>();

            if (cachedPlayerInputs != null)
            {
                isPlayerInZone = true;
                cachedPlayer = other.gameObject;
                Debug.Log("[DungeonEntry] Player entered trigger zone.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || LayerMask.LayerToName(other.gameObject.layer) == "Player")
        {
            isPlayerInZone = false;
            cachedPlayer = null;
            cachedPlayerInputs = null;
            Debug.Log("[DungeonEntry] Player exited trigger zone.");
        }
    }

    private IEnumerator ProcessEntryRoutine(GameObject player)
    {
        isPlayerInZone = false;
        cachedPlayerInputs = null;

        bool useCutscene = (entryCamera != null);

        if (useCutscene)
        {
            entryCamera.Priority = 20;
            if (leftDoor) leftDoor.DOLocalRotate(new Vector3(0, -90, 0), 1.5f).SetEase(Ease.OutQuad);
            if (rightDoor) rightDoor.DOLocalRotate(new Vector3(0, 90, 0), 1.5f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(sequenceDuration);
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (!string.IsNullOrEmpty(targetSceneName) && targetSceneName != SceneManager.GetActiveScene().name)
        {
            _pendingLocationName = targetLocationName;
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            TeleportPlayer(player, targetLocationName);

            if (useCutscene)
            {
                yield return new WaitForSeconds(0.5f);
                entryCamera.Priority = 0;
                CloseDoors();
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(_pendingLocationName)) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player) StartCoroutine(WaitAndTeleport(player));

        _pendingLocationName = null;
    }

    private IEnumerator WaitAndTeleport(GameObject player)
    {
        yield return null;
        TeleportPlayer(player, _pendingLocationName);
    }

    private void TeleportPlayer(GameObject player, string rootName)
    {
        GameObject rootObj = GameObject.Find(rootName);
        if (rootObj == null)
        {
            Debug.LogError($"[DungeonEntry] Destination root '{rootName}' not found in scene.");
            return;
        }

        int myId = 0;
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
            myId = NetworkManager.Instance.MyPlayerId;

        string targetSpawnName = rootName.Contains("DungeonEntrance") ? "DungeonEntrance_spawn" : $"Playerspawn{myId + 1}";

        Transform targetPoint = FindChildByName(rootObj.transform, targetSpawnName);

        if (targetPoint != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;

            player.transform.position = targetPoint.position;
            player.transform.rotation = targetPoint.rotation;

            if (cc) cc.enabled = true;

            Debug.Log($"[DungeonEntry] Teleported {player.name} to {targetSpawnName} (ID: {myId})");
        }
        else
        {
            Debug.LogWarning($"[DungeonEntry] Spawn point '{targetSpawnName}' not found. Moving to root object position.");
            player.transform.position = rootObj.transform.position;
        }
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindChildByName(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void CloseDoors()
    {
        if (leftDoor) leftDoor.DOLocalRotate(Vector3.zero, 1f);
        if (rightDoor) rightDoor.DOLocalRotate(Vector3.zero, 1f);
    }
}