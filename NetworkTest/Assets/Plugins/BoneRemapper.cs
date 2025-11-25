using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 남성 -> 여성 장신구나 모델을 복사해서 사용하려고 할때, Bone Remapping이 필요함.
/// [ 사용법 ]
/// 아래 changeSkinnedMeshRenderer : 복사한 모델.
///     targetRootBone : 복사한 모델을 사용할 모델의 root본 (hips)
/// 를 할당후 Bone Remapper 컴포넌트 우클릭후 Bone Remap 클릭.
/// </summary>

public class BoneRemapper : MonoBehaviour
{
    // 복사해온(문제가 있는) 장신구의 SkinMeshRenderer
    [SerializeField] private SkinnedMeshRenderer changeSkinnedMeshRenderer;

    // 갈아끼울 대상이 되는 루트 본 (예: 여성 모델의 Hips 혹은 최상위 부모)
    [SerializeField] private Transform targetRootBone;

    [ContextMenu("Remap Bones")]
    public void RemapBones()
    {
        if (changeSkinnedMeshRenderer == null || targetRootBone == null)
        {
            Debug.LogError("SMR 또는 Target Root Bone이 설정되지 않았습니다.");
            return;
        }

        // 1. 타겟(여성) 모델의 모든 뼈를 이름으로 찾기 쉽게 딕셔너리에 담음
        Transform[] newBoneTransforms = targetRootBone.GetComponentsInChildren<Transform>(true);
        Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();

        foreach (var bone in newBoneTransforms)
        {
            if (!boneMap.ContainsKey(bone.name))
            {
                boneMap.Add(bone.name, bone);
            }
        }

        // 2. 장신구가 현재 참조하고 있는 뼈(남성의 뼈) 목록을 가져옴
        Transform[] oldBones = changeSkinnedMeshRenderer.bones;
        Transform[] newBones = new Transform[oldBones.Length];

        // 3. 이름 매칭을 통해 여성의 뼈로 교체
        for (int i = 0; i < oldBones.Length; i++)
        {
            // 기존 뼈가 null이거나, 타겟에 같은 이름의 뼈가 있다면 교체
            if (oldBones[i] != null && boneMap.TryGetValue(oldBones[i].name, out Transform foundBone))
            {
                newBones[i] = foundBone;
            }
            else
            {
                // 매칭되는 뼈가 없으면 기존 것을 유지하거나(위험), 경고 출력
                // 보통 손가락 끝이나 더미 본 등에서 발생할 수 있음
                newBones[i] = oldBones[i];
                Debug.LogWarning($"뼈를 찾을 수 없음: {oldBones[i]?.name}. 렌더링이 깨질 수 있습니다.");
            }
        }

        // 4. 최종적으로 새로운 뼈 배열 할당
        changeSkinnedMeshRenderer.bones = newBones;

        // 5. RootBone도 교체 (보통 Hips)
        if (boneMap.TryGetValue(changeSkinnedMeshRenderer.rootBone.name, out Transform newRoot))
        {
            changeSkinnedMeshRenderer.rootBone = newRoot;
        }

        Debug.Log("Bone Remapping 완료!");
    }
}