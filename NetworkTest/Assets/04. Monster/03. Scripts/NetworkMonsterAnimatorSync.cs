using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(MonsterAIController))]
public class NetworkMonsterAnimatorSync : MonoBehaviour
{
    private Animator animator;
    private MonsterAIController monsterAIController;

    // Host-side: A list to store triggers that have fired since the last network update.
    private List<int> pendingTriggers = new List<int>();

    // A simple data structure to hold all the animation data for synchronization.
    public struct MonsterAnimationData
    {
        public float moveSpeed;
        public List<int> triggeredHashes;
        // Add other parameters like idleType, isBlocking etc. as needed.
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
        monsterAIController = GetComponent<MonsterAIController>();

        // 클라이언트에서는 루트 모션을 비활성화하여 위치 동기화 스크립트와의 충돌을 방지합니다.
        // 위치는 NetworkMonsterTransformSync가 전적으로 제어해야 합니다.
        if (NetworkManager.Instance != null && NetworkManager.Instance.Mode == NetworkMode.Client)
        {
            animator.applyRootMotion = false;
        }
    }

    void OnEnable()
    {
        // Only the host should listen for local animation triggers.
        // A proper network identity check (e.g., isHost, isServer) is needed here.
        // For now, we assume this script on the host will handle this.
        if (NetworkManager.Instance != null && NetworkManager.Instance.Mode == NetworkMode.Host)
        {
            monsterAIController.OnAnimatorTriggered += HandleAnimatorTriggered;
        }
    }

    void OnDisable()
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.Mode == NetworkMode.Host)
        {
            monsterAIController.OnAnimatorTriggered -= HandleAnimatorTriggered;
        }
    }

    /// <summary>
    /// Called by the MonsterAIController's event on the host when an animation trigger is fired.
    /// </summary>
    private void HandleAnimatorTriggered(int animHash)
    {
        // Add the trigger hash to our list of pending triggers to be sent.
        if (!pendingTriggers.Contains(animHash))
        {
            pendingTriggers.Add(animHash);
        }
    }

    /// <summary>
    /// [HOST-SIDE] Gathers the current animation state and any pending triggers into a data packet.
    /// This should be called by a network manager script at a regular interval.
    /// </summary>
    public MonsterAnimationData GetAnimationData()
    {
        var data = new MonsterAnimationData
        {
            moveSpeed = animator.GetFloat(monsterAIController.hashMoveSpeed),
            triggeredHashes = new List<int>(pendingTriggers)
        };

        // Clear the pending triggers list after gathering them.
        pendingTriggers.Clear();

        return data;
    }

    /// <summary>
    /// [CLIENT-SIDE] Receives animation data from the host and applies it to the local animator.
    /// This should be called by a network manager script when it receives an update.
    /// </summary>
    public void OnAnimationDataReceived(MonsterAnimationData data)
    {
        if (animator == null) return;

        // Apply the state parameters.
        animator.SetFloat(monsterAIController.hashMoveSpeed, data.moveSpeed);

        // Fire off the triggers.
        if (data.triggeredHashes != null)
        {
            foreach (int hash in data.triggeredHashes)
            {
                animator.SetTrigger(hash);
            }
        }
    }

    #region Serialization Helpers

    /// <summary>
    /// [HOST-SIDE] Serializes the MonsterAnimationData struct into a byte array for network transport.
    /// </summary>
    public static byte[] Serialize(MonsterAnimationData data)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(data.moveSpeed);
            writer.Write((byte)(data.triggeredHashes?.Count ?? 0));
            if (data.triggeredHashes != null)
            {
                foreach (int hash in data.triggeredHashes)
                {
                    writer.Write(hash);
                }
            }
            return stream.ToArray();
        }
    }

    /// <summary>
    /// [CLIENT-SIDE] Deserializes a byte array back into a MonsterAnimationData struct.
    /// </summary>
    public static MonsterAnimationData Deserialize(byte[] bytes)
    {
        var data = new MonsterAnimationData();
        data.triggeredHashes = new List<int>();

        if (bytes == null || bytes.Length == 0) return data;

        using (MemoryStream stream = new MemoryStream(bytes))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            data.moveSpeed = reader.ReadSingle();
            byte triggerCount = reader.ReadByte();
            for (int i = 0; i < triggerCount; i++)
            {
                data.triggeredHashes.Add(reader.ReadInt32());
            }
        }
        return data;
    }

    #endregion
}
