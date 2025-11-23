using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Steamworks;
using System.Linq;

// A manager for scene objects that need their state synchronized.
// This is for objects that are part of the scene, not dynamically spawned entities like players.
public class WorldInteractableManager : MonoBehaviour
{
    public static WorldInteractableManager Instance { get; private set; }

    // This list must be populated in the Unity Editor Inspector.
    // It should contain all objects in the scene that implement IWorldInteractable.
    [SerializeField]
    private List<MonoBehaviour> registeredInteractables;

    private readonly Dictionary<string, IWorldInteractable> interactableMap = new Dictionary<string, IWorldInteractable>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // This should likely live in the scene, not persist across loads.
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeInteractables();

        NetworkManager.OnJsonMessageReceived += HandleJsonMessage;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnJsonMessageReceived -= HandleJsonMessage;
        }
    }

    /// <summary>
    /// Iterates through the pre-configured list, assigns a unique and consistent ID (its index),
    /// and registers the interactable object.
    /// </summary>
    private void InitializeInteractables()
    {
        if (registeredInteractables == null) return;

        for (int i = 0; i < registeredInteractables.Count; i++)
        {
            if (registeredInteractables[i] is IWorldInteractable interactable)
            {
                string id = i.ToString();
                interactable.Initialize(id);
                if (!interactableMap.ContainsKey(id))
                {
                    interactableMap.Add(id, interactable);
                }
                else
                {
                    Debug.LogWarning($"[WorldInteractableManager] Duplicate ID detected: {id}. Object: {registeredInteractables[i].gameObject.name}. This may be due to a misconfigured list.", registeredInteractables[i].gameObject);
                }
            }
            else
            {
                Debug.LogWarning($"[WorldInteractableManager] Object at index {i} ({registeredInteractables[i].name}) does not implement IWorldInteractable.", registeredInteractables[i].gameObject);
            }
        }
    }

    /// <summary>
    /// Handles incoming JSON messages from the network.
    /// </summary>
    private void HandleJsonMessage(CSteamID sender, string jsonMsg)
    {
        JObject msg;
        try
        {
            msg = JObject.Parse(jsonMsg);
        }
        catch (System.Exception)
        {
            // Not a valid JSON message, ignore.
            return;
        }

        string type = msg["type"]?.ToString();

        // Host receives a request from a client to change an object's state.
        if (type == "interactable_state_change" && NetworkManager.Instance.Mode == NetworkMode.Host)
        {
            HandleStateChangeRequest(msg);
        }
        // All clients (including host) receive an authoritative state update from the host.
        else if (type == "interactable_state_update")
        {
            HandleStateUpdateRequest(msg);
        }
    }

    /// <summary>
    /// [HOST ONLY] Processes a client's request to interact with an object.
    /// It determines the new state and broadcasts it to all clients.
    /// </summary>
    private void HandleStateChangeRequest(JObject msg)
    {
        string id = msg["id"]?.ToString();
        string subId = msg["subId"]?.ToString(); // subId for multi-part objects

        if (string.IsNullOrEmpty(id) || !interactableMap.ContainsKey(id))
        {
            Debug.LogWarning($"[Host] Received state change request for unknown ID: {id}");
            return;
        }

        var interactable = interactableMap[id];
        JToken newState = interactable.GetState(subId);

        if (newState == null) return;

        JObject updateMsg = new JObject
        {
            ["type"] = "interactable_state_update",
            ["id"] = id,
            ["subId"] = subId,
            ["state"] = newState
        };

        NetworkManager.Instance.BroadcastJsonMessage(updateMsg);
    }

    /// <summary>
    /// [ALL CLIENTS] Receives an authoritative state update from the host and applies it.
    /// </summary>
    private void HandleStateUpdateRequest(JObject msg)
    {
        string id = msg["id"]?.ToString();
         string subId = msg["subId"]?.ToString();

        if (string.IsNullOrEmpty(id) || !interactableMap.ContainsKey(id))
        {
            Debug.LogWarning($"[Client] Received state update for unknown ID: {id}");
            return;
        }

        JToken state = msg["state"];
        interactableMap[id].SetState(subId, state);
    }

    /// <summary>
    /// Called by a local player's interaction. It informs the authoritative instance (host or self)
    /// that an interaction has occurred.
    /// </summary>
    public void RequestStateChange(string fullId)
    {
        string[] parts = fullId.Split('_');
        string id = parts[0];
        string subId = parts.Length > 1 ? parts[1] : null;

        if (NetworkManager.Instance.Mode == NetworkMode.Client)
        {
            JObject requestMsg = new JObject
            {
                ["type"] = "interactable_state_change",
                ["id"] = id,
                ["subId"] = subId
            };
            NetworkManager.Instance.SendJsonMessage(NetworkManager.Instance.LobbyHostID, requestMsg);
        }
        else 
        {
             if (string.IsNullOrEmpty(id) || !interactableMap.ContainsKey(id))
            {
                Debug.LogWarning($"[WorldInteractableManager] Interactable with ID '{id}' not found for RequestStateChange.");
                return;
            }

            var interactable = interactableMap[id];
            JToken newState = interactable.GetState(subId);
            if (newState == null) return;

            if (NetworkManager.Instance.Mode == NetworkMode.SinglePlayer)
            {
                 interactable.SetState(subId, newState);
            }
            else if (NetworkManager.Instance.Mode == NetworkMode.Host)
            {
                 JObject updateMsg = new JObject
                {
                    ["type"] = "interactable_state_update",
                    ["id"] = id,
                    ["subId"] = subId,
                    ["state"] = newState
                };
                NetworkManager.Instance.BroadcastJsonMessage(updateMsg);
            }
        }
    }
}


/// <summary>
/// Interface for scene objects whose state needs to be synchronized over the network.
/// </summary>
public interface IWorldInteractable
{
    void Initialize(string id);
    void SetState(string subId, JToken state);
    JToken GetState(string subId);
}
