using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ChatManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private UnityEvent chatSendFunction;
    private PlayerInputs playerInputs;

    private bool isChatFocus = false;

    void Start()
    {
        playerInputs = GameManager.Instance.GetComponent<PlayerInputs>();
    }

    void Update()
    {
        if (playerInputs.GetChatOpen())
        {
            /*
            Enter 입력 -> if 채팅창 focus가 true라면? inputfield에 text가 있는지 확인 -> message보내기
                    -> if 채탕창 focus가 false라면? focus해주기.
            */

            if (isChatFocus)
            {
                if (!string.IsNullOrEmpty(chatInputField.text))
                {
                    chatSendFunction.Invoke();
                }
                chatInputField.DeactivateInputField();
                GameManager.Instance.SetPause(false);
            }
            else
            {
                GameManager.Instance.SetPause(true);
                chatInputField.ActivateInputField();
            }
            isChatFocus = !isChatFocus;
        }
    }
}
