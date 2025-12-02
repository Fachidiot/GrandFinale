using UnityEngine;
using UnityEngine.UI;

public class MonsterHPBar : MonoBehaviour
{
    [Header("참조 (비워두면 자동 연결됨)")]
    public Slider hpSlider;          // UI 슬라이더
    public MonsterHealth health;     // 몬스터 체력 스크립트

    [Header("설정")]
    public bool alwaysFaceCamera = true; // 카메라 바라보기 여부
    public float lerpSpeed = 5f;         // 체력이 부드럽게 깎이는 속도

    private Transform mainCam;

    void Start()
    {
        // 1. 메인 카메라 캐싱
        if (Camera.main != null) mainCam = Camera.main.transform;

        // 2. Health 자동 연결 (부모들 중에서 찾음)
        if (health == null)
        {
            health = GetComponentInParent<MonsterHealth>();
        }

        // 3. Slider 자동 연결 (내 몸이나 자식들 중에서 찾음)
        if (hpSlider == null)
        {
            // 1순위: 이 스크립트가 붙은 오브젝트에 슬라이더가 있는지 확인
            hpSlider = GetComponent<Slider>();

            // 2순위: 없다면 자식 오브젝트들(예: HealthBar) 뒤져서 찾기
            if (hpSlider == null)
            {
                hpSlider = GetComponentInChildren<Slider>();
            }
        }

        // 4. 슬라이더 값 초기화 (연결 성공 시)
        if (health != null && hpSlider != null)
        {
            hpSlider.maxValue = health._maxHP;
            hpSlider.value = health.CurrentHP;
        }
        else
        {
            // 찾지 못했을 경우 경고 로그
            // Debug.LogWarning($"[MonsterHPBar] {name}: Health 또는 Slider를 찾을 수 없습니다.");
        }
    }

    void LateUpdate()
    {
        // 1. 빌보드 처리 (카메라 정면 바라보기)
        if (alwaysFaceCamera && mainCam != null)
        {
            // 카메라와 똑같은 회전값을 가짐 (가장 깔끔한 빌보드 방식)
            transform.rotation = mainCam.rotation;
        }

        // 2. 체력 업데이트
        if (health != null && hpSlider != null)
        {
            // 최대 체력 동기화 (레벨업 등으로 변동 가능성 대비)
            hpSlider.maxValue = health._maxHP;

            // 부드럽게 체력바 감소 (Lerp)
            hpSlider.value = Mathf.Lerp(hpSlider.value, health.CurrentHP, Time.deltaTime * lerpSpeed);
        }
    }
}