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

        List<string> currentPlayers = new List<string>(players.Keys);
        foreach (string playerId in currentPlayers)
        {
            if (!playerIdsInMessage.Contains(playerId))
            {
                Destroy(players[playerId]);
                players.Remove(playerId);
            }
        }

        foreach (JObject playerInfo in playerList.Cast<JObject>())
        {
            string playerId = playerInfo["player_id"].ToString();
            if (!players.ContainsKey(playerId))
            {
                SpawnPlayer(playerId, Vector3.zero);
            }
        }
    }

    private void SpawnPlayer(string playerId, Vector3 position)
    {
        if (playerPrefab == null)
            return;

        GameObject playerObject = Instantiate(playerPrefab, position, Quaternion.identity);
        playerObject.name = $"Player_{playerId}";
        players.Add(playerId, playerObject);

        bool isMine = (playerId == NetworkManager.Instance.PlayerId);

        // Initialize Transform Sync components
        NetworkTransformSync[] transformSyncs = playerObject.GetComponentsInChildren<NetworkTransformSync>();
        foreach (var view in transformSyncs)
        {
            view.Initialize(playerId, isMine);
        }

        // Initialize Animator Sync component
        NetworkAnimatorSync animSync = playerObject.GetComponentInChildren<NetworkAnimatorSync>();
        if (animSync != null)
        {
            animSync.Initialize(playerId, isMine);
        }

        if (isMine)
        {
            // Local player setup
        }
        else
        {
            // Remote player setup
            playerObject.GetComponent<CharacterMove>().enabled = false;
            playerObject.GetComponentInChildren<InputHandler>().enabled = false;
            playerObject.GetComponentInChildren<CameraController>().enabled = false;
            playerObject.GetComponentInChildren<CameraSwitcher>()?.gameObject.SetActive(false);
        }

        DontDestroyOnLoad(playerObject);
    }

    public void UpdatePlayersState(JArray playersState)
    {
        if (playersState == null)
            return;

        foreach (JObject playerInfo in playersState.Cast<JObject>())
        {
            string playerId = playerInfo["player_id"]?.ToString();

            if (players.TryGetValue(playerId, out GameObject playerObject))
            {
                // --- Handle Transform Sync ---
                NetworkTransformSync[] transformSyncs = playerObject.GetComponentsInChildren<NetworkTransformSync>();
                if (transformSyncs.Length > 0)
                {
                    JObject bodyPosJson = playerInfo["body_pos"] as JObject;
                    Vector3 bodyPosition = new Vector3(
                        bodyPosJson["x"].Value<float>(),
                        bodyPosJson["y"].Value<float>(),
                        bodyPosJson["z"].Value<float>());

                    JObject bodyRotJson = playerInfo["body_rot"] as JObject;
                    Quaternion bodyRotation = new Quaternion(
                        bodyRotJson["x"].Value<float>(),
                        bodyRotJson["y"].Value<float>(),
                        bodyRotJson["z"].Value<float>(),
                        bodyRotJson["w"].Value<float>());

                    JObject camRotJson = playerInfo["cam_rot"] as JObject;
                    Quaternion camRotation = new Quaternion(
                        camRotJson["x"].Value<float>(),
                        camRotJson["y"].Value<float>(),
                        camRotJson["z"].Value<float>(),
                        camRotJson["w"].Value<float>());

                    foreach (var view in transformSyncs)
                    {
                        if (view.viewId == 0)
                            view.OnTransformReceived(bodyPosition, bodyRotation);
                        else if (view.viewId == 1)
                            view.OnTransformReceived(view.transform.position, camRotation);
                    }
                }

                // --- Handle Animator Sync ---
                NetworkAnimatorSync animSync = playerObject.GetComponentInChildren<NetworkAnimatorSync>();
                if (animSync != null)
                {
                    float x = playerInfo["x"].Value<float>();
                    float y = playerInfo["y"].Value<float>();

                    bool walk = playerInfo["walk"].Value<bool>();
                    bool sprint = playerInfo["sprint"].Value<bool>();
                    bool roll = playerInfo["roll"].Value<bool>();
                    bool isGrounded = playerInfo["isGrounded"].Value<bool>();
                    bool crouch = playerInfo["crouch"].Value<bool>();

                    animSync.OnAnimationDataReceived(x, y, walk, sprint, roll, isGrounded, crouch);
                }

                // --- Handle Weapon State Sync ---
                WeaponController weaponController = playerObject.GetComponentInChildren<WeaponController>();
                if (weaponController != null)
                {
                    int weaponId = playerInfo["weapon_id"].Value<int>();
                    if (weaponController.activeID != weaponId)
                    {
                        weaponController.ToChange(weaponId);
                    }
                }
            }
        }
    }

    public void RoutePlayerEvent(JObject eventData)
    {
        string playerId = eventData["player_id"]?.ToString();
        if (playerId == NetworkManager.Instance.PlayerId)
            return;

        if (players.TryGetValue(playerId, out GameObject playerObject))
        {
            Debug.Log($"{playerObject.name} : event invoke");
            NetworkStateMachine nsm = playerObject.GetComponentInChildren<NetworkStateMachine>();
            if (nsm != null)
            {
                nsm.OnNetworkEvent(eventData);
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
    }
}
