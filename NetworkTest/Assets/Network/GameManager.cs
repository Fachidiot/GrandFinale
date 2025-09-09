
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Newtonsoft.Json.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject playerPrefab;
    private Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    private CinemachineVirtualCamera playerCamera;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdatePlayerList(JArray playerList)
    {
        List<string> playerIdsInMessage = playerList.Select(p => p["player_id"].ToString()).ToList();

        // Remove players who are no longer in the room
        List<string> currentPlayers = new List<string>(players.Keys);
        foreach (string playerId in currentPlayers)
        {
            if (!playerIdsInMessage.Contains(playerId))
            {
                Destroy(players[playerId]);
                players.Remove(playerId);
            }
        }

        // Add or update players
        foreach (JObject playerInfo in playerList.Cast<JObject>())
        {
            string playerId = playerInfo["player_id"].ToString();
            JObject posJson = playerInfo["position"] as JObject;
            Vector3 position = new Vector3(posJson["x"].Value<float>(), posJson["y"].Value<float>(), posJson["z"].Value<float>());

            if (!players.ContainsKey(playerId))
            {
                SpawnPlayer(playerId, position);
            }
        }
    }

    private void SpawnPlayer(string playerId, Vector3 position)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab is not set in GameManager.");
            return;
        }

        GameObject playerObject = Instantiate(playerPrefab, position, Quaternion.identity);
        playerObject.name = $"Player_{playerId}";
        players.Add(playerId, playerObject);

        if (playerId == NetworkManager.Instance.PlayerId)
        {
            playerObject.AddComponent<PlayerController>();
            SetupThirdPersonCamera(playerObject);
        }
        else
        {
            playerObject.AddComponent<NetworkedPlayerController>();
        }
    }

    private void SetupThirdPersonCamera(GameObject target)
    {
        if (playerCamera == null)
        {
            GameObject camObj = new GameObject("PlayerFollowCamera");
            playerCamera = camObj.AddComponent<CinemachineVirtualCamera>();
        }

        playerCamera.m_Follow = target.transform;
        playerCamera.m_LookAt = target.transform;

        // Configure the camera for a 3rd person view
        var transposer = playerCamera.AddCinemachineComponent<CinemachineTransposer>();
        transposer.m_FollowOffset = new Vector3(0, 1.5f, -5); // Adjust as needed

        // Ensure the main camera has a CinemachineBrain
        if (Camera.main != null && Camera.main.GetComponent<CinemachineBrain>() == null)
        {
            Camera.main.gameObject.AddComponent<CinemachineBrain>();
        }
    }

    public void UpdatePlayerPosition(string playerId, Vector3 position)
    {
        if (players.TryGetValue(playerId, out GameObject playerObject))
        {
            NetworkedPlayerController controller = playerObject.GetComponent<NetworkedPlayerController>();
            if (controller != null)
            {
                controller.SetTargetPosition(position);
            }
        }
    }

    public void ClearPlayers()
    {
        foreach (var player in players.Values)
        {
            Destroy(player);
        }
        players.Clear();

        if (playerCamera != null)
        {
            Destroy(playerCamera.gameObject);
            playerCamera = null;
        }
    }
}
