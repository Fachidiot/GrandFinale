using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public enum InteractType
{
    None,
    Door,
    Closet_Left,
    Closet_Right,
    Craft,
    ShipDoor,
    Dungeon,
}

public class Interactable : MonoBehaviour
{
    [Header("Settings")]
    public InteractType interactType;
    public string interactionText = "Interact";
    public KeyCode interactCode = KeyCode.F;

    private bool toggle = false;

    void Awake()
    {
        if (interactType == InteractType.None)
        {
            if (string.IsNullOrEmpty(interactionText))
                interactionText = "Interact" + $"\n[{interactCode}]";
            else
                interactionText += $"\n[{interactCode}]";
            return;
        }
        TextUpdate();
    }

    [Header("Events")]
    [SerializeField] private UnityEvent<GameObject> onInteract;

    public void Interact(GameObject gameObject = null)
    {
        onInteract.Invoke(gameObject);
        toggle = !toggle;
        TextUpdate();
    }

    private void TextUpdate()
    {
        switch (interactType)
        {
            case InteractType.Door:
                if (!toggle)
                    interactionText = "문 열기" + $"\n[{interactCode}]";
                else
                    interactionText = "문 닫기" + $"\n[{interactCode}]";
                break;
            case InteractType.Closet_Right:
                if (!toggle)
                    interactionText = "오른쪽 옷장 열기" + $"\n[{interactCode}]";
                else
                    interactionText = "오른쪽 옷장 닫기" + $"\n[{interactCode}]";
                break;
            case InteractType.Closet_Left:
                if (!toggle)
                    interactionText = "왼쪽 옷장 열기" + $"\n[{interactCode}]";
                else
                    interactionText = "왼쪽 옷장 닫기" + $"\n[{interactCode}]";
                break;
            case InteractType.Craft:
                if (!toggle)
                    interactionText = "상자 열기" + $"\n[{interactCode}]";
                else
                    interactionText = "상자 닫기" + $"\n[{interactCode}]";
                break;
            case InteractType.ShipDoor:
                if (toggle)
                    interactionText = "함선 문 열기" + $"\n[{interactCode}]";
                else
                    interactionText = "함선 문 닫기" + $"\n[{interactCode}]";
                break;
            case InteractType.Dungeon:
                if (!toggle)
                    interactionText = "던전 입장" + $"\n[{interactCode}]";
                else
                    interactionText = "던전 탈출" + $"\n[{interactCode}]";
                break;
        }
    }
}