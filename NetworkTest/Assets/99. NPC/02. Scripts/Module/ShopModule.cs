using UnityEngine;
using System;

[DisallowMultipleComponent]
public class ShopModule : MonoBehaviour
{
    #region ====== [ UI 설정 ] =================================================
    [Header("UI")]
    [Tooltip("상점 UI 패널 오브젝트(비활성 시작 권장)")]
    [SerializeField] public GameObject shopPanel;

    [Tooltip("열 때 마우스 커서를 보이고, 닫을 때 이전 상태로 복구")]
    [SerializeField] private bool lockCursor = true;

    [Space(6)]
    [Tooltip("런타임에 패널을 자동으로 찾아 바인딩할지 여부")]
    [SerializeField] private bool autoFindPanel = true;

    [Tooltip("태그로 먼저 탐색")]
    [SerializeField] private string panelTag = "ShopUI";

    [Tooltip("태그로 못 찾으면 이름으로 탐색")]
    [SerializeField] private string panelNameFallback = "ShopUIPanel";
    #endregion

    #region ====== [ 입력/자동 닫힘 정책 ] ======================================
    [Header("자동 닫힘/입력")]
    [Tooltip("플레이어가 이 거리 이상 멀어지면 자동으로 닫기")]
    [SerializeField] private float autoCloseDistance = 5f;
    public float AutoCloseDistance => autoCloseDistance;

    [Tooltip("ESC 로 닫기 허용")]
    [SerializeField] private bool closeWithEsc = true;

    // ESC 전용으로 단순화 → E(Interact) 닫기는 비활성
    [SerializeField, HideInInspector] private bool closeWithInteract = false;

    // (ESC 전용이라서 실사용 안 함. 유지만.)
    [SerializeField, HideInInspector] private float interactCloseDelay = 0.4f;
    #endregion

    #region ====== [ 디버그 ] ===================================================
    [Header("디버그")]
    [SerializeField] private bool debugLogs = true;
    private void DLog(string msg)
    {
        if (debugLogs) Debug.Log($"[ShopModule:{name}] {msg}");
    }
    #endregion

    #region ====== [ 상태 / 이벤트 ] ===========================================
    public bool IsOpen { get; private set; }

    public event Action OnShopOpened;
    public event Action OnShopClosed;

    // 누가 열었는지(보통 플레이어)
    private Transform opener;

    // 매 프레임 GetComponent 방지: 열린 순간에만 캐싱
    private PlayerInputHandler openerIH;

    // 열림 시각(ESC 전용이라 기능상 필요 없지만 유지)
    private float openedAt = -999f;

    // 커서 상태 복구용
    private CursorLockMode prevLock;
    private bool prevCursorVisible;
    #endregion

    #region ====== [ 라이프사이클 ] ============================================
    private void Awake()
    {
        TryAutoFindPanel();
        if (!IsOpen && shopPanel) shopPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (!IsOpen && shopPanel) shopPanel.SetActive(false);
    }

    private void OnDisable()
    {
        if (IsOpen)
        {
            DLog("OnDisable: 강제 닫기");
            InternalClose(force: true);
        }
    }

    private void OnDestroy()
    {
        if (IsOpen)
        {
            DLog("OnDestroy: 강제 닫기");
            InternalClose(force: true);
        }
    }
    #endregion

    #region ====== [ 공개 API ] ================================================
    /// <summary>열려 있으면 닫고, 닫혀 있으면 연다.</summary>
    public void ToggleShop(Transform by)
    {
        if (IsOpen) CloseShop();
        else OpenShop(by);
    }

    /// <summary>상점 열기 (플레이어 Transform 필요)</summary>
    public void OpenShop(Transform by)
    {
        if (IsOpen)
        {
            DLog("열기 무시: 이미 열려 있음");
            return;
        }

        if (!shopPanel) TryAutoFindPanel();

        IsOpen = true;
        opener = by;
        openedAt = Time.time;

        CacheOpenerInputHandler();      // 열린 순간 한 번만 캐시

        if (shopPanel) shopPanel.SetActive(true);
        else DLog("경고: shopPanel 미할당 — UI 표시 안 됨");

        if (lockCursor)
        {
            prevLock = Cursor.lockState;
            prevCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        FreezeOpener(true);             // 게임플레이 입력 억제(ESC는 살림)
        OnShopOpened?.Invoke();
        DLog("열림");
    }

    /// <summary>상점 닫기</summary>
    public void CloseShop()
    {
        if (!IsOpen)
        {
            DLog("닫기 무시: 이미 닫혀 있음");
            return;
        }

        InternalClose(force: false);
        DLog("닫힘");
    }

    /// <summary>외부에서 패널을 런타임으로 연결할 때 사용</summary>
    public void BindPanel(GameObject panel)
    {
        shopPanel = panel;
        if (!IsOpen && shopPanel) shopPanel.SetActive(false);
        DLog($"패널 바인딩: {(panel ? panel.name : "NULL")}");
    }
    #endregion

    #region ====== [ 메인 루프 ] ===============================================
    private void Update()
    {
        if (!IsOpen) return;

        // 1) 입력으로 닫기 (ESC 전용)
        if (ShouldCloseByInput())
        {
            DLog("입력(ESC)으로 닫기");
            CloseShop();
            return;
        }

        // 2) 거리 초과 자동 닫기
        if (ShouldCloseByDistance())
        {
            DLog("거리 초과로 닫기");
            CloseShop();
            return;
        }
    }
    #endregion

    #region ====== [ 내부 로직 ] ===============================================
    /// <summary>ESC로 닫을지 판단 (E 닫기는 비활성)</summary>
    private bool ShouldCloseByInput()
    {
        // 상점이 열리자마자는 바로 닫지 못하게 0.2초의 지연을 줍니다.
        if (Time.time < openedAt + 0.2f) return false;

        if (openerIH == null) return false;

        // ESC로만 닫기
        if (closeWithEsc && openerIH.ShopClosePressed)
            return true;

        return false;
    }

    /// <summary>플레이어가 너무 멀어졌는지 검사</summary>
    private bool ShouldCloseByDistance()
    {
        if (!opener) return false;
        return Vector3.Distance(opener.position, transform.position) >= autoCloseDistance;
    }

    /// <summary>오프너의 PlayerInputHandler를 캐시(매 프레임 GetComponent 방지)</summary>
    private void CacheOpenerInputHandler()
    {
        openerIH = null;
        if (!opener) return;

        openerIH = opener.GetComponent<PlayerInputHandler>();
        if (!openerIH) DLog("참고: opener에 PlayerInputHandler가 없음");
    }

    /// <summary>플레이어 입력 억제/해제 (이동/공격 등 — ESC는 허용)</summary>
    private void FreezeOpener(bool freeze)
    {
        if (!opener) return;

        if (openerIH)
        {
            openerIH.SuppressGameplayInput = freeze;
            DLog($"플레이어 입력 억제 {(freeze ? "ON" : "OFF")}");
        }

        var rb = opener.GetComponent<Rigidbody>();
        if (rb && freeze)
        {
            rb.velocity = Vector3.zero;
            DLog("플레이어 속도 0 처리");
        }
    }

    /// <summary>공통 닫기 처리(커서/입력/상태/이벤트 정리)</summary>
    private void InternalClose(bool force)
    {
        if (shopPanel) shopPanel.SetActive(false);

        if (lockCursor)
        {
            Cursor.lockState = prevLock;
            Cursor.visible = prevCursorVisible;
        }

        FreezeOpener(false);

        IsOpen = false;
        OnShopClosed?.Invoke();

        opener = null;
        openerIH = null;
    }

    /// <summary>shopPanel 자동 탐색(태그 → 이름 순)</summary>
    private void TryAutoFindPanel()
    {
        if (!autoFindPanel || shopPanel) return;

        // 1) Tag로 찾기
        if (!string.IsNullOrEmpty(panelTag))
        {
            try
            {
                var byTag = GameObject.FindWithTag(panelTag);
                if (byTag)
                {
                    shopPanel = byTag;
                    DLog($"패널 자동 할당(Tag): {byTag.name}");
                }
            }
            catch
            {
                DLog($"참고: 태그 '{panelTag}' 가 정의되지 않음");
            }
        }

        // 2) 이름으로 찾기 (태그로 못 찾았을 때만)
        if (!shopPanel && !string.IsNullOrEmpty(panelNameFallback))
        {
            var byName = GameObject.Find(panelNameFallback);
            if (byName)
            {
                shopPanel = byName;
                DLog($"패널 자동 할당(Name): {byName.name}");
            }
        }

        if (!shopPanel)
            DLog("경고: 자동 패널 탐색 실패 — 인스펙터에서 직접 지정 권장");
    }
    #endregion
}
