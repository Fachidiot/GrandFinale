using UnityEngine;
using System.Collections.Generic;

public class LootOrbVisuals : MonoBehaviour
{
    [Header("��޺� VFX ������ (�ڽ� ������Ʈ)")]
    public GameObject vfxCommon;
    public GameObject vfxRare;
    public GameObject vfxEpic;


    // [���߰���] VFX���� ���δ� �θ� ������Ʈ (�߽��� ����)
    // �� �ʵ带 GenericLootDrop �������� Inspector���� VFX_Center ������Ʈ�� �����ؾ� �մϴ�.
    [Header("�߽��� ������Ʈ")]
    public GameObject vfxCenter;

    // ������ ���� ��ƼŬ �ý��� ������Ʈ�� �̸� ĳ���մϴ�.
    private ParticleSystem commonPS;
    private ParticleSystem rarePS;
    private ParticleSystem epicPS;


    private void Awake()
    {
        // 1. Awake���� ParticleSystem ������Ʈ�� �̸� ã�Ƽ� ĳ���մϴ�.
        if (vfxCommon) commonPS = vfxCommon.GetComponent<ParticleSystem>();
        if (vfxRare) rarePS = vfxRare.GetComponent<ParticleSystem>();
        if (vfxEpic) epicPS = vfxEpic.GetComponent<ParticleSystem>();
    }

    public void Initialize(string grade)
    {
        Debug.Log($"<color=yellow>[VFX_INIT] Initialize ����. ��û ���: {grade}</color>");
        // 1. ������ ���� ��� VFX ������Ʈ�� ��Ȱ��ȭ�մϴ�. 
        if (vfxCommon) vfxCommon.SetActive(false);
        if (vfxRare) vfxRare.SetActive(false);
        if (vfxEpic) vfxEpic.SetActive(false);

        ParticleSystem targetPS = null;
        GameObject targetVFXObject = null;

        // 2. ��޿� �´� VFX ����
        switch (grade)
        {
            case "Common":
                if (vfxCommon) { targetVFXObject = vfxCommon; targetPS = commonPS; }
                break;
            case "Rare":
                if (vfxRare) { targetVFXObject = vfxRare; targetPS = rarePS; }
                break;
            case "Epic":
                if (vfxEpic) { targetVFXObject = vfxEpic; targetPS = epicPS; }
                break;
            default:
                if (vfxCommon) { targetVFXObject = vfxCommon; targetPS = commonPS; }
                break;
        }

        if (targetVFXObject != null)
        {
            Debug.Log($"<color=yellow>[VFX_INIT] ���õ� VFX ������Ʈ: {targetVFXObject.name}</color>");
            Debug.Log($"<color=yellow>[VFX_INIT] ParticleSystem ĳ�� ����: {(targetPS != null ? "OK" : "NULL")}</color>");

            // 3. VFX ������Ʈ Ȱ��ȭ
            targetVFXObject.SetActive(true);

            // [�ڷα� �߰� 4��] Ȱ��ȭ �õ� ����
            Debug.Log($"<color=yellow>[VFX_INIT] {targetVFXObject.name}.SetActive(true) ȣ�� �Ϸ�.</color>");


            // 4. [���ٽɡ�] ParticleSystem.Play()�� ���������� ȣ��
            if (targetPS != null)
            {
                targetPS.Play();
                Debug.Log($"<color=yellow>[VFX_INIT] {targetVFXObject.name} - ParticleSystem.Play() ȣ�� �Ϸ�.</color>");
            }
            else
            {
                Debug.LogError($"[VFX ERROR] '{grade}' ��� VFX ������Ʈ�� ParticleSystem ������Ʈ�� �����ϴ�.");
            }
        }
        else
        {
            Debug.LogError($"[VFX ERROR] '{grade}' ��޿� �ش��ϴ� VFX �������� ������� �ʾҽ��ϴ�.");
        }

        // [�ڷα� �߰� 5��] �߽��� ������Ʈ ���� Ȯ�� (Ȱ��ȭ ���� ���ܿ�)
        if (vfxCenter)
        {
            Debug.Log($"<color=yellow>[VFX_INIT] VFX Center '{vfxCenter.name}' Active State: {vfxCenter.activeInHierarchy}</color>");
        }

        Debug.Log($"[VFX DEBUG] {targetVFXObject.name} activeSelf={targetVFXObject.activeSelf}, activeInHierarchy={targetVFXObject.activeInHierarchy}");

    }
}