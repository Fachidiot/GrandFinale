using UnityEngine;

// 이동(NavMesh) 로직 완전 제거
public abstract class NPCBrain : MonoBehaviour, INPC
{
    [Header("Core")]
    [SerializeField] protected string id;
    [SerializeField] protected NPCKind kind;

    [Header("Visuals")]
    [SerializeField] protected Animator animator;

    public string Id => id;
    public NPCKind Kind => kind;

    protected virtual void Awake()
    {
        // ID가 비어있으면 자동 생성
        if (string.IsNullOrEmpty(id))
            id = System.Guid.NewGuid().ToString("N");

        if (!animator) animator = GetComponentInChildren<Animator>(true);

        // 매니저에 자신을 등록 (씬에 미리 배치된 경우를 위해 Start나 Awake에서 등록)
        NPCManager.Instance?.RegisterNPC(this);
    }

    protected virtual void OnDestroy()
    {
        NPCManager.Instance?.UnregisterNPC(id);
    }
}