using UnityEngine;
using System.Collections.Generic;

public class LootOrbVisuals : MonoBehaviour
{
    [Header("등급별 VFX 프리팹 (파티클 시스템)")]
    public GameObject vfxCommon;
    public GameObject vfxRare;
    public GameObject vfxEpic;


    // 부모 오브젝트인 GenericLootDrop 프리팹의 Inspector에서 VFX_Center 오브젝트를 할당해줘야 함.
    [Header("파티클 부모 오브젝트")]
    public GameObject vfxCenter;

    // 파티클을 직접 제어하기 위해 파티클 시스템을 캐싱해 둠.
    private ParticleSystem commonPS;
    private ParticleSystem rarePS;
    private ParticleSystem epicPS;


    private void Awake()
    {
        // 1. Awake에서 ParticleSystem 컴포넌트를 미리 찾아 캐싱해 둠.
        if (vfxCommon) commonPS = vfxCommon.GetComponent<ParticleSystem>();
        if (vfxRare) rarePS = vfxRare.GetComponent<ParticleSystem>();
        if (vfxEpic) epicPS = vfxEpic.GetComponent<ParticleSystem>();
    }

    public void Initialize(string grade)
    {
        // 1. 시작하기 전에 모든 VFX 오브젝트를 비활성화. 
        if (vfxCommon) vfxCommon.SetActive(false);
        if (vfxRare) vfxRare.SetActive(false);
        if (vfxEpic) vfxEpic.SetActive(false);

        ParticleSystem targetPS = null;
        GameObject targetVFXObject = null;

        // 2. 등급에 맞는 VFX 선택
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

            // 3. VFX 오브젝트 활성화
            targetVFXObject.SetActive(true);

            if (targetPS != null)
            {
                targetPS.Play();
            }
            else
            {
                Debug.LogError($"[VFX ERROR] '{grade}' 등급 VFX 오브젝트에 ParticleSystem 컴포넌트가 없습니다.");
            }
        }
        else
        {
            Debug.LogError($"[VFX ERROR] '{grade}' 등급에 해당하는 VFX 프리팹이 할당되지 않았습니다.");
        }

    }
}