using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float health = 100f;
    [SerializeField] private PlayerLifeController playerLifeController;
    [SerializeField] private HitBoxColidersList hitBoxColidersList;
    [SerializeField] private bool Networked = true;

    private bool isMine = false;
    private InGameUIManager uiManager;

    private void Start()
    {
        if (Networked)
            isMine = transform.root.GetComponentInChildren<NetworkTransformSync>().IsMine;
        hitBoxColidersList.Init();
        Init();
    }

    void Update()
    {
        if (isMine && !uiManager)
            Init();
    }

    public float SetDamage(float damage)
    {
        if (!isMine)
            return 0;
        if (!uiManager)
            Init();

        health -= damage;

        float retValue = health;

        if (Networked)
        {
            if (!transform.root.GetComponent<NetworkTransformSync>().IsMine)
            {
                uiManager.SetHealthValue(health);
            }
        }
        else
            uiManager.SetHealthValue(health);

        if (health <= 0)
        {
            playerLifeController.Die();

            health = 100f;
        }

        return retValue;
    }

    private void Init()
    {
        uiManager = FindObjectOfType<InGameUIManager>();
        if (uiManager)
            uiManager.SetHealthValue(health);
    }
}
