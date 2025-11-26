using UnityEngine;
using System.Collections.Generic;

public class BoneRemapper : MonoBehaviour
{
    [Header("모델들이 모여있는 부모 객체 (이 아래의 모든 메쉬를 찾음)")]
    public Transform characterModelRoot;

    [Header("새로운 아마추어의 최상위 부모 (Armature_new)")]
    public Transform newArmatureRoot;

    [ContextMenu("모든 파츠 뼈대 일괄 교체 (Remap All)")]
    public void RemapAllParts()
    {
        if (characterModelRoot == null || newArmatureRoot == null)
        {
            Debug.LogError("모델 루트와 새로운 아마추어 Root를 모두 연결해주세요!");
            return;
        }

        // 1. 새로운 뼈대 정보를 딕셔너리에 저장 (한 번만 수행하여 최적화)
        Dictionary<string, Transform> newBoneMap = new Dictionary<string, Transform>();
        Transform[] allNewBones = newArmatureRoot.GetComponentsInChildren<Transform>(true);

        foreach (Transform bone in allNewBones)
        {
            if (!newBoneMap.ContainsKey(bone.name))
            {
                newBoneMap.Add(bone.name, bone);
            }
        }

        // 2. 모델 루트 아래의 모든 SkinnedMeshRenderer 찾기
        SkinnedMeshRenderer[] allRenderers = characterModelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        if (allRenderers.Length == 0)
        {
            Debug.LogWarning("해당 객체 아래에서 SkinnedMeshRenderer를 하나도 찾지 못했습니다.");
            return;
        }

        Debug.Log($"총 {allRenderers.Length}개의 파츠를 발견했습니다. 교체 작업을 시작합니다...");

        // 3. 각 렌더러(파츠)마다 뼈 교체 작업 수행
        foreach (var meshRenderer in allRenderers)
        {
            RemapSingleRenderer(meshRenderer, newBoneMap);
        }

        Debug.Log("모든 파츠의 뼈대 교체가 완료되었습니다!");
    }

    // 개별 렌더러 처리 로직
    private void RemapSingleRenderer(SkinnedMeshRenderer targetMesh, Dictionary<string, Transform> newBoneMap)
    {
        Transform[] oldBones = targetMesh.bones;
        Transform[] newBones = new Transform[oldBones.Length];
        int missingCount = 0;

        for (int i = 0; i < oldBones.Length; i++)
        {
            string boneName = oldBones[i].name;

            if (newBoneMap.TryGetValue(boneName, out Transform foundBone))
            {
                newBones[i] = foundBone;
            }
            else
            {
                newBones[i] = oldBones[i]; // 못 찾으면 유지
                missingCount++;
            }
        }

        // Root Bone 교체
        if (targetMesh.rootBone != null && newBoneMap.TryGetValue(targetMesh.rootBone.name, out Transform newRoot))
        {
            targetMesh.rootBone = newRoot;
        }

        // 최종 적용
        targetMesh.bones = newBones;

        if (missingCount > 0)
            Debug.LogWarning($"[{targetMesh.name}] : {missingCount}개의 뼈를 찾지 못해 기존 뼈를 유지했습니다.");
    }
}