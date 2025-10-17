using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float health = 100f;
    [SerializeField] private PlayerLifeController playerLifeController;
    [SerializeField] private HitBoxColidersList hitBoxColidersList;

    private InGameUIManager uiManager;

    private void Start()
    {
        hitBoxColidersList.Init();
        uiManager = FindObjectOfType<InGameUIManager>();
    }

    public float SetDamage(float damage)
    {
        health -= damage;

        float retValue = health;

        uiManager.SetHealthValue(health);
        // if (transform.root.GetComponent<NetworkTransformSync>().IsMine)
        // {
        //     uiManager.SetHealthValue(health);
        // }

        if (health <= 0)
        {
            playerLifeController.Die();

            health = 100f;
        }

        return retValue;
    }
}
