// Assets/Scripts/Managers/PlayerAbilityManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WeaponController))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerAbilityManager : MonoBehaviour
{
    // --- �ʼ� ������Ʈ ���� ---
    private WeaponController weaponController;
    private PlayerStats playerStats;
    private CharacterMove characterMove;

    // --- ��ų ���� ���� ---
    private Dictionary<string, Coroutine> runningSkillCoroutines = new Dictionary<string, Coroutine>();
    private Dictionary<string, bool> skillCooldowns = new Dictionary<string, bool>();

    //  1. (�߰�) ���� ������ ����(�нú�) ����Ʈ 
    private List<RelicData> equippedRelics = new List<RelicData>();
    // (Aura ���� �нú� ����Ʈ ������ ���� ����Ʈ)
    private List<GameObject> passiveEffectInstances = new List<GameObject>();


    void Awake()
    {
        // �ʼ� ������Ʈ ã�ƿ���
        weaponController = GetComponent<WeaponController>();
        playerStats = GetComponent<PlayerStats>();
        characterMove = GetComponent<CharacterMove>();


        if (playerStats == null) Debug.LogError("PlayerStats�� �����ϴ�. (�нú� ���� �Ұ�)");
    }



    // [�׽�Ʈ��] Ű �Է�
    void Update()
    {
        // �ڡڡ� (�߰�) G/H Ű�� ������ �߰�/���� �׽�Ʈ �ڡڡ�
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("REL_001 ����");
            AddRelic("REL_001"); // (����) �ִ� ü�� ����
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("REL_001 ����");
            RemoveRelic("REL_001"); // (����) �ִ� ü�� ���� (����)
        }

        // KŰ�� '���� źâ' �ߵ� �׽�Ʈ
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("ABIL_007 ����");
            TryActivateAbility("ABIL_007");
        }

        // LŰ�� '������ �ǵ�' �ߵ� �׽�Ʈ
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("ABIL_006 ����");
            TryActivateAbility("ABIL_006");
        }
    }

    // ====================================================================
    // �ڡڡ� 2. (�ű�) ������ �߰� / ���� (���� �Լ�) ��
    // ====================================================================

    /// <summary>
    /// (ItemPickup.cs�� ȣ��)
    /// �÷��̾�� ������ �߰��ϰ� ������ �����մϴ�.
    /// </summary>
    public void AddRelic(string itemID)
    {
        if (!DataManager.Instance.RelicDB.TryGetValue(itemID, out RelicData relic))
        {
            Debug.LogWarning($"[AbilityManager] {itemID} RelicData�� ã�� �� ����");
            return;
        }

        // (�ߺ� ȹ�� ���� ���� ��... )
        if (equippedRelics.Contains(relic))
        {
            equippedRelics.Add(relic);
            Debug.Log($"[AbilityManager] {relic.itemName}��(��) �̹� ���� ���Դϴ�.");
            return;
        }

        equippedRelics.Add(relic);
        Debug.Log($"[AbilityManager] {relic.itemName} ȹ��.");

        // ���� ����!
        RecalculateAllPassiveStats();
    }

    /// <summary>
    /// (������ ������ ����� ȣ��)
    /// �÷��̾�Լ� ������ �����ϰ� ������ �����մϴ�.
    /// </summary>
    public void RemoveRelic(string itemID)
    {
        if (!DataManager.Instance.RelicDB.TryGetValue(itemID, out RelicData relic)) return;

        if (equippedRelics.Remove(relic))
        {
            Debug.Log($"[AbilityManager] {relic.itemName} ����.");
            // ���� ����!
            RecalculateAllPassiveStats();
        }
    }

    // ====================================================================
    // �ڡڡ� 3. (���׷��̵�) ���� ���� ���� �ڡڡ�
    // (���� ApplyPassiveAbility �Լ��� ��ü)
    // ====================================================================

    /// <summary>
    /// ������ ��� ����(Relic)�� ������� �нú� ����/�ɷ��� ó������ �ٽ� �����մϴ�.
    /// </summary>
    private void RecalculateAllPassiveStats()
    {
        // --- 1. ���� �нú� ȿ�� ��� ���� ---
        StopAndClearAllPassiveEffects();

        // --- 2. PlayerStats�� �⺻������ ���� ---
        if (playerStats == null) return;
        playerStats.ResetToBaseStats();

        // --- 3. ������ ��� ������ ��ȸ�ϸ� �нú� ���� ---
        foreach (RelicData relic in equippedRelics)
        {
            AbilityData ability = relic.grantedAbility;
            if (ability != null && ability.activationType == "Passive")
            {
                ApplyPassiveLogic(ability);
            }
        }

        // (�ʿ�� ü�� ����ȭ - �ִ� ü���� �پ��� �� ���� ü���� �� ������ �� ��)
        playerStats.ValidateHealth();
        // (PlayerStats.cs�� ValidateHealth() { if (CurrentHealth > CurrentMaxHealth) CurrentHealth = CurrentMaxHealth; } �߰� �ʿ�)
    }

    /// <summary>
    /// �нú� �ɷ� 1���� ������ ������ �����մϴ�.
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

            case "Aura_Heal_Ally":
            case "Aura_Damage_Enemy":
                Debug.Log($"{ability.abilityName} ���� ���� (���� �ʿ�)");
                if (!string.IsNullOrEmpty(ability.resourcePath))
                {
                    // GameObject prefab = ... (�����Ϳ��� �ε�)
                    // GameObject instance = Instantiate(prefab, transform);
                    // passiveEffectInstances.Add(instance); // �����Ÿ� ���� ����Ʈ�� �߰�
                }
                break;

            default:
                Debug.LogWarning($"���ǵ��� ���� �нú� ����: {ability.abilityLogicID}");
                break;
        }
    }

    /// <summary>
    /// ���� ���� ���� ��� ����/����Ʈ�� ���߰� �ı��մϴ�.
    /// </summary>
    private void StopAndClearAllPassiveEffects()
    {
        foreach (GameObject instance in passiveEffectInstances)
        {
            if (instance != null) Destroy(instance);
        }
        passiveEffectInstances.Clear();
        // (�� �ܿ� �нú� �ڷ�ƾ�� �ִٸ� StopCoroutine...)
    }


    // ====================================================================
    // 4. ��Ƽ�� �ɷ� �ߵ� (���� �ڵ�� ����)
    // ====================================================================
    public void TryActivateAbility(string abilityID)
    {
        Debug.Log("ABIL_007 '����źâ' ��ų ����");


        // 1. ��Ÿ�� Ȯ��
        if (skillCooldowns.TryGetValue(abilityID, out bool onCooldown) && onCooldown)
        {
            Debug.Log($"[{abilityID}] ��ų ��Ÿ�� ���Դϴ�.");
            return;
        }

        // 2. ������ ��������
        if (!DataManager.Instance.AbilityDB.TryGetValue(abilityID, out AbilityData ability))
        {
            Debug.LogWarning($"[{abilityID}] AbilityData�� ã�� �� ����.");
            return;
        }
        if (ability.activationType != "Active") return;

        // 3. ���� ID�� ���� ��ų �ڷ�ƾ ����
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
                // �ڡڡ� (����) PlayerStats�� �Լ��� ���� ȣ���ϵ��� ���� �ڡڡ�
                float.TryParse(ability.param_Key, out float shieldAmount); // 100
                float.TryParse(ability.param_ValueA, out float duration); // 10
                float.TryParse(ability.param_ValueB, out float cooldown); // 20

                if (playerStats != null)
                {
                    playerStats.AddTemporaryShield(shieldAmount, duration); // �� �ǵ� ����
                    StartCoroutine(CooldownRoutine(ability.abilityID, cooldown)); // �� ��Ÿ�Ӹ� ����
                }
                break;

            case "Cone_Knockback": // (ABIL_005)
                // (�˹��� ��߼��̹Ƿ� �ڷ�ƾ�� �ƴ� ���� ����)
                // ExecuteKnockback(ability);
                Debug.Log("�˹� ��ų �ߵ� (���� �ʿ�)");
                break;

            default:
                Debug.LogWarning($"���ǵ��� ���� ��Ƽ�� ����: {ability.abilityLogicID}");
                break;
        }

        // 4. ���õ� ��ų �ڷ�ƾ ���� (Add_Shield�� ����)
        if (skillRoutine != null)
        {
            StartSkillCoroutine(abilityID, skillRoutine);
        }
    }

    // ====================================================================
    // 5. ��ų ���� (�ڷ�ƾ) (���� �ڵ�� ����)
    // ====================================================================

    /**
     * ABIL_007: (����) ���� źâ
     */
    private IEnumerator InfiniteAmmoRoutine(AbilityData ability)
    {
        // --- 1. ������ �Ľ� �� ��Ÿ�� ���� ---
        float.TryParse(ability.param_ValueA, out float duration); // 10
        float.TryParse(ability.param_ValueB, out float cooldown); // 60
        StartCoroutine(CooldownRoutine(ability.abilityID, cooldown)); // ��Ÿ�� ��� ����

        Debug.Log($"[{ability.abilityName}] ��ų Ȱ��ȭ! (����: {duration}��)");
        // (����Ʈ ����: ability.resourcePath)

        // --- 2. ��ų ���� (�ٽ�) ---
        float timer = 0f;
        while (timer < duration)
        {
            Weapon currentWeapon = weaponController.GETCurrentWeapon;

            if (currentWeapon != null)
            {
                currentWeapon.InfiniteAmmo();
            }

            timer += Time.deltaTime;
            yield return null; // ���� �����ӱ��� ���
        }

        // --- 3. ��ų ���� ---
        Debug.Log($"[{ability.abilityName}] ��ų ����.");
        // (����Ʈ ����)
        runningSkillCoroutines.Remove(ability.abilityID);
    }

    /**
     * ABIL_006: (����) ������ �ǵ�
     * (PlayerStats.AddTemporaryShield�� �̵������Ƿ� �� �ڷ�ƾ�� ���� �ʿ� ����)
     */
    // private IEnumerator AddShieldRoutine(AbilityData ability) { ... } // �ڡڡ� ������ �ڡڡ�


    // ====================================================================
    // 6. ��ƿ��Ƽ (�ڷ�ƾ ����) (���� �ڵ�� ����)
    // ====================================================================

    // ��ų �ڷ�ƾ ���� (�ߺ� ����)
    private void StartSkillCoroutine(string abilityID, IEnumerator routine)
    {
        if (runningSkillCoroutines.ContainsKey(abilityID))
        {
            StopCoroutine(runningSkillCoroutines[abilityID]);
            runningSkillCoroutines.Remove(abilityID);
        }
        runningSkillCoroutines.Add(abilityID, StartCoroutine(routine));
    }

    // ��Ÿ�� �ڷ�ƾ
    private IEnumerator CooldownRoutine(string abilityID, float cooldownTime)
    {
        skillCooldowns[abilityID] = true; // ��Ÿ�� ����
        Debug.Log($"[{abilityID}] ��Ÿ�� ����: {cooldownTime}��");

        yield return new WaitForSeconds(cooldownTime);

        skillCooldowns[abilityID] = false; // ��Ÿ�� ����
        Debug.Log($"[{abilityID}] ��Ÿ�� ����.");
    }
}