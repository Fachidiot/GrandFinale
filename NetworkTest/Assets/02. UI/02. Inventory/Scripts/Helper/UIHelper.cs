using UnityEngine;

public static class UIHelper
{
    public static T FindChild<T>(Transform parent, string name) where T : Component
    {
        // 1. 바로 아래 자식들 검색
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child.GetComponent<T>();
            }

            // 2. 없으면 자식의 자식(재귀) 검색
            T result = FindChild<T>(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    // 내 자식들 중에서 이름으로 GameObject 찾기
    public static GameObject FindObject(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child.gameObject;
            }

            GameObject result = FindObject(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}