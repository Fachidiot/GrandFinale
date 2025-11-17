using UnityEngine;
using System.Collections.Generic;

public class LootOrbVisuals : MonoBehaviour
{
    [Header("��޺� VFX ������ (�ڽ� ������Ʈ)")]
    public GameObject vfxCommon;
    public GameObject vfxRare;
    public GameObject vfxEpic;


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

            // 3. VFX ������Ʈ Ȱ��ȭ
            targetVFXObject.SetActive(true);

            if (targetPS != null)
            {
                targetPS.Play();
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

    }
}