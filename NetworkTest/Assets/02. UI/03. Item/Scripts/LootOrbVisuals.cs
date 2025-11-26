using UnityEngine;
using System.Collections.Generic;

public class LootOrbVisuals : MonoBehaviour
{
    [Header("등급별 VFX 프리팹 (파티클 시스템)")]
    public GameObject vfxCommon;
    public GameObject vfxUncommon; 
    public GameObject vfxRare;
    public GameObject vfxEpic;
    public GameObject vfxLegendary;
    public GameObject vfxMythic;   


    [Header("파티클 부모 오브젝트")]
    public GameObject vfxCenter;

    private ParticleSystem commonPS;
    private ParticleSystem uncommonPS; 
    private ParticleSystem rarePS;
    private ParticleSystem epicPS;
    private ParticleSystem legendaryPS; 
    private ParticleSystem mythicPS;    


    private void Awake()
    {
        // 1. Awake에서 ParticleSystem 컴포넌트를 미리 찾아 캐싱해 둠.
        if (vfxCommon) commonPS = vfxCommon.GetComponent<ParticleSystem>();
        if (vfxUncommon) uncommonPS = vfxUncommon.GetComponent<ParticleSystem>();
        if (vfxRare) rarePS = vfxRare.GetComponent<ParticleSystem>();
        if (vfxEpic) epicPS = vfxEpic.GetComponent<ParticleSystem>();
        if (vfxLegendary) legendaryPS = vfxLegendary.GetComponent<ParticleSystem>(); 
        if (vfxMythic) mythicPS = vfxMythic.GetComponent<ParticleSystem>(); 
    }

    public void Initialize(string grade)
    {
        if (vfxCommon) vfxCommon.SetActive(false);
        if (vfxUncommon) vfxUncommon.SetActive(false); 
        if (vfxRare) vfxRare.SetActive(false);
        if (vfxEpic) vfxEpic.SetActive(false);
        if (vfxLegendary) vfxLegendary.SetActive(false); 
        if (vfxMythic) vfxMythic.SetActive(false); 

        ParticleSystem targetPS = null;
        GameObject targetVFXObject = null;

        switch (grade)
        {
            case "Common":
                if (vfxCommon) { targetVFXObject = vfxCommon; targetPS = commonPS; }
                break;
            case "Uncommon":
                if (vfxUncommon) { targetVFXObject = vfxUncommon; targetPS = uncommonPS; }
                break;
            case "Rare":
                if (vfxRare) { targetVFXObject = vfxRare; targetPS = rarePS; }
                break;
            case "Epic":
                if (vfxEpic) { targetVFXObject = vfxEpic; targetPS = epicPS; }
                break;
            case "Legendary": 
                if (vfxLegendary) { targetVFXObject = vfxLegendary; targetPS = legendaryPS; }
                break;
            case "Mythic":
                if (vfxMythic) { targetVFXObject = vfxMythic; targetPS = mythicPS; }
                break;
            default:
                Debug.LogWarning($"[LootVisuals] 알 수 없는 등급: {grade}. Common VFX로 대체합니다.");
                if (vfxCommon) { targetVFXObject = vfxCommon; targetPS = commonPS; }
                break;
        }

        if (targetVFXObject != null)
        {
            targetVFXObject.SetActive(true);

            if (targetPS != null)
            {
                targetPS.Play();
            }
            else
            {
                Debug.LogWarning($"[VFX Warning] '{grade}' 등급 VFX 오브젝트에 ParticleSystem 컴포넌트가 없습니다. 오브젝트만 활성화합니다.");
            }
        }
        else
        {
            if (grade != "None") 
            {
                Debug.LogWarning($"[VFX Warning] '{grade}' 등급에 해당하는 VFX 프리팹이 인스펙터에 할당되지 않았습니다.");
            }
        }
    }
}