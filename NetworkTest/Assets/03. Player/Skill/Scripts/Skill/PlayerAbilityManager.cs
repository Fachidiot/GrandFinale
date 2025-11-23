// Assets/Scripts/Managers/PlayerAbilityManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WeaponController))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerAbilityManager : MonoBehaviour
{
    // --- 필수 컴포넌트 참조 ---
    private WeaponController weaponController;
    private PlayerStats playerStats;
    private CharacterMove characterMove;

    // --- 스킬 관리 변수 ---
    private Dictionary<string, Coroutine> runningSkillCoroutines = new Dictionary<string, Coroutine>();
    private Dictionary<string, bool> skillCooldowns = new Dictionary<string, bool>();

    //  1. (추가) 장착 유물 목록(패시브) 리스트 
    private List<RelicData> equippedRelics = new List<RelicData>();
    // (Aura 같은 패시브 이펙트 오브젝트를 담는 리스트)
    private List<GameObject> passiveEffectInstances = new List<GameObject>();


    void Awake()
    {
        // 필수 컴포넌트 찾아오기
        weaponController = GetComponent<WeaponController>();
        playerStats = GetComponent<PlayerStats>();
        characterMove = GetComponent<CharacterMove>();


        if (playerStats == null) Debug.LogError("PlayerStats가 없습니다. (패시브 적용 불가)");
    }



    // [테스트용] 키 입력
    void Update()
    {
        // TODO (추가) G/H 키로 유물 추가/삭제 테스트 TODO
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("REL_001 장착");
            AddRelic("REL_001"); // (유물) 최대 체력 증가
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("REL_001 해제");
            RemoveRelic("REL_001"); // (유물) 최대 체력 증가 (해제)
        }

        // K키로 '무한 탄창' 발동 테스트
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("ABIL_007 사용");
            TryActivateAbility("ABIL_007");
        }

        // L키로 '수호의 방패' 발동 테스트
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("ABIL_006 사용");
            TryActivateAbility("ABIL_006");
        }
    }

    // ====================================================================
    // TODO 2. (신규) 유물 추가 / 해제 (관리 함수) 들
    // ====================================================================

    /// <summary>
    /// (ItemPickup.cs가 호출)
    /// 플레이어에게서 유물을 추가하고 스탯을 갱신합니다.
    /// </summary>
    public void AddRelic(string itemID)
    {
        if (!DataManager.Instance.RelicDB.TryGetValue(itemID, out RelicData relic))
        {
            Debug.LogWarning($"[AbilityManager] {itemID} RelicData를 찾을 수 없음");
            return;
        }

        // (중복 획득 방지 로직 등... )
        if (equippedRelics.Contains(relic))
        {
            equippedRelics.Add(relic);
            Debug.Log($"[AbilityManager] {relic.itemName}은(는) 이미 소지 중입니다.");
            // return; // 주석처리: 중복 소지 시 스탯 중첩 적용되도록 함
        }

        equippedRelics.Add(relic);
        Debug.Log($"[AbilityManager] {relic.itemName} 획득.");

        // 스탯 갱신!
        RecalculateAllPassiveStats();
    }

    /// <summary>
    /// (나중에 상점 시스템에서 호출)
    /// 플레이어에게서 유물을 제거하고 스탯을 갱신합니다.
    /// </summary>
    public void RemoveRelic(string itemID)
    {
        if (!DataManager.Instance.RelicDB.TryGetValue(itemID, out RelicData relic)) return;

        if (equippedRelics.Remove(relic))
        {
            Debug.Log($"[AbilityManager] {relic.itemName} 해제.");
            // 스탯 갱신!
            RecalculateAllPassiveStats();
        }
    }

    // ====================================================================
    // TODO 3. (업그레이드) 패시브 스탯 적용 TODO
    // (기존 ApplyPassiveAbility 함수를 대체)
    // ====================================================================

    /// <summary>
    /// 장착한 모든 유물(Relic)을 기반으로 패시브 스탯/어빌리티 처리를 다시 계산합니다.
    /// </summary>
    private void RecalculateAllPassiveStats()
    {
        // --- 1. 기존 패시브 효과 모두 초기화 ---
        StopAndClearAllPassiveEffects();

        // --- 2. PlayerStats를 기본값으로 리셋 ---
        if (playerStats == null) return;
        playerStats.ResetToBaseStats();

        // --- 3. 장착한 모든 유물을 순회하며 패시브 적용 ---
        foreach (RelicData relic in equippedRelics)
        {
            AbilityData ability = relic.grantedAbility;
            if (ability != null && ability.activationType == "Passive")
            {
                ApplyPassiveLogic(ability);
            }
        }

        // (필요시 체력 동기화 - 최대 체력이 줄었을 때 현재 체력이 더 높을 수 있음)
        playerStats.ValidateHealth();
        // (PlayerStats.cs에 ValidateHealth() { if (CurrentHealth > CurrentMaxHealth) CurrentHealth = CurrentMaxHealth; } 추가 필요)
    }

    /// <summary>
    /// 패시브 어빌리티 1개의 로직을 실제로 적용합니다.
    /// </summary>
    private void ApplyPassiveLogic(AbilityData ability)
    {
        if (playerStats == null) return;

        switch (ability.abilityLogicID)
        {
            case "Stat_Add":
                // Key="MaxHealth", ValueA="50"
                playerStats.AddStat(ability.param_Key, float.Parse(ability.param_ValueA));
                break;

            case "Stat_Percent":
                // Key="MoveSpeed", ValueA="10"
                playerStats.AddStatPercent(ability.param_Key, float.Parse(ability.param_ValueA));
                break;

            // TODO 1. (신규) 복합 스탯 적용 케이스 추가 TODO
            case "Stat_Composite":
                // 이 어빌리티는 param_Key에 따라 여러 스탯을 변경합니다.
                ApplyCompositeStat(ability);
                break;

            case "Aura_Heal_Ally":
            case "Aura_Damage_Enemy":
                Debug.Log($"{ability.abilityName} 오라 생성 (구현 필요)");
                if (!string.IsNullOrEmpty(ability.resourcePath))
                {
                    // GameObject prefab = ... (어드레서블에서 로드)
                    // GameObject instance = Instantiate(prefab, transform);
                    // passiveEffectInstances.Add(instance); // 파티클/오라 관리 리스트에 추가
                }
                break;

            default:
                Debug.LogWarning($"정의되지 않은 패시브 로직: {ability.abilityLogicID}");
                break;
        }
    }

    // TODO 2. (신규) 복합 스탯 적용 함수 TODO
    /// <summary>
    /// 'Stat_Composite' 로직 ID를 가진 어빌리티의 세부 스탯을 적용합니다.
    /// </summary>
    private void ApplyCompositeStat(AbilityData ability)
    {
        switch (ability.param_Key)
        {
            // ABIL_301: S.A.S 전투 모듈
            // (체력, 방어, 이동 속도, 공격력 동시 증가)
            case "SAS_Module":
                playerStats.AddStat("MaxHealth", 10f);
                playerStats.AddStat("Defense", 5f);
                playerStats.AddStat("Power", 5f);
                playerStats.AddStatPercent("MoveSpeed", 5f); // 5% 증가
                break;

            // ABIL_302: '벌워크' 철갑갑옷
            // (최대 체력/방어력 대폭 증가, 이동 속도 감소)
            case "Bulwark_Armor":
                playerStats.AddStat("MaxHealth", 50f);
                playerStats.AddStat("Defense", 20f);
                playerStats.AddStatPercent("MoveSpeed", -15f); // 15% 감소
                break;

            default:
                Debug.LogWarning($"[ApplyCompositeStat] 정의되지 않은 param_Key: {ability.param_Key}");
                break;
        }
    }


    /// <summary>
    /// 모든 오라/이펙트 등 오브젝트/컴포넌트를 깨끗이 파괴합니다.
    /// </summary>
    private void StopAndClearAllPassiveEffects()
    {
        foreach (GameObject instance in passiveEffectInstances)
        {
            if (instance != null) Destroy(instance);
        }
        passiveEffectInstances.Clear();
        // (그 외의 패시브 코루틴이 있다면 StopCoroutine...)
    }


    // ====================================================================
    // 4. 액티브 어빌리티 발동 (기존 코드와 동일)
    // ====================================================================
    public void TryActivateAbility(string abilityID)
    {
        Debug.Log("ABIL_007 '무한탄창' 스킬 사용");


        // 1. 쿨타임 확인
        if (skillCooldowns.TryGetValue(abilityID, out bool onCooldown) && onCooldown)
        {
            Debug.Log($"[{abilityID}] 스킬 쿨타임 중입니다.");
            return;
        }

        // 2. 데이터베이스에서 어빌리티 찾기
        if (!DataManager.Instance.AbilityDB.TryGetValue(abilityID, out AbilityData ability))
        {
            Debug.LogWarning($"[{abilityID}] AbilityData를 찾을 수 없음.");
            return;
        }
        if (ability.activationType != "Active") return;

        // 3. 로직 ID에 따라 스킬 코루틴 선택
        IEnumerator skillRoutine = null;

        switch (ability.abilityLogicID)
        {
            case "Apply_Self_Buff":
                if (ability.param_Key == "InfiniteAmmo") // (ABIL_007)
                {
                    skillRoutine = InfiniteAmmoRoutine(ability);
                }
                break;

            case "Add_Shield": // (ABIL_006)
                // TODO (수정) PlayerStats의 함수를 직접 호출하도록 변경 TODO
                float.TryParse(ability.param_Key, out float shieldAmount); // 100
                float.TryParse(ability.param_ValueA, out float duration); // 10
                float.TryParse(ability.param_ValueB, out float cooldown); // 20

                if (playerStats != null)
                {
                    playerStats.AddTemporaryShield(shieldAmount, duration); // 방패만 적용
                    StartCoroutine(CooldownRoutine(ability.abilityID, cooldown)); // 쿨타임만 적용
                }
                break;

            case "Cone_Knockback": // (ABIL_005)
                // (즉발형이므로 코루틴이 아닌 즉시 실행)
                // ExecuteKnockback(ability);
                Debug.Log("넉백 스킬 발동 (구현 필요)");
                break;

            default:
                Debug.LogWarning($"정의되지 않은 액티브 로직: {ability.abilityLogicID}");
                break;
        }

        // 4. 선택된 스킬 코루틴 실행 (Add_Shield는 제외)
        if (skillRoutine != null)
        {
            StartSkillCoroutine(abilityID, skillRoutine);
        }
    }

    // ====================================================================
    // 5. 스킬 로직 (코루틴) (기존 코드와 동일)
    // ====================================================================

    /**
     * ABIL_007: (스킬) 무한 탄창
     */
    private IEnumerator InfiniteAmmoRoutine(AbilityData ability)
    {
        // --- 1. 데이터 파싱 및 쿨타임 적용 ---
        float.TryParse(ability.param_ValueA, out float duration); // 10
        float.TryParse(ability.param_ValueB, out float cooldown); // 60
        StartCoroutine(CooldownRoutine(ability.abilityID, cooldown)); // 쿨타임 코루틴 시작

        Debug.Log($"[{ability.abilityName}] 스킬 활성화! (지속시간: {duration}초)");
        // (이펙트 생성: ability.resourcePath)

        // --- 2. 스킬 효과 (지속시간) ---
                    float timer = 0f;
                    while (timer < duration)
                    {
                        RangedWeapon currentWeapon = weaponController.GETCurrentWeapon as RangedWeapon;
        
                        if (currentWeapon != null) // Add null check after cast
                        {
                            currentWeapon.InfiniteAmmo();
                        }
        
                        timer += Time.deltaTime;
                        yield return null; // 한 프레임씩 대기
                    }
        // --- 3. 스킬 종료 ---
        Debug.Log($"[{ability.abilityName}] 스킬 종료.");
        // (이펙트 파괴)
        runningSkillCoroutines.Remove(ability.abilityID);
    }



    // ====================================================================
    // 6. 유틸리티 (코루틴 관리) (기존 코드와 동일)
    // ====================================================================

    // 스킬 코루틴 관리 (중복 실행 방지)
    private void StartSkillCoroutine(string abilityID, IEnumerator routine)
    {
        if (runningSkillCoroutines.ContainsKey(abilityID))
        {
            StopCoroutine(runningSkillCoroutines[abilityID]);
            runningSkillCoroutines.Remove(abilityID);
        }
        runningSkillCoroutines.Add(abilityID, StartCoroutine(routine));
    }



    // 쿨타임 코루틴
    private IEnumerator CooldownRoutine(string abilityID, float cooldownTime)
    {
        skillCooldowns[abilityID] = true; // 쿨타임 시작
        Debug.Log($"[{abilityID}] 쿨타임 시작: {cooldownTime}초");

        yield return new WaitForSeconds(cooldownTime);

        skillCooldowns[abilityID] = false; // 쿨타임 종료
        Debug.Log($"[{abilityID}] 쿨타임 종료.");
    }
}
