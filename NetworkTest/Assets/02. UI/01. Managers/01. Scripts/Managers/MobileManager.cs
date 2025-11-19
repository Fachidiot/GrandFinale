using UnityEngine;

public class MobileManager : MonoBehaviour
{
    [SerializeField] private GameObject player;
    public GameObject Player { get { return player; } }
    void Awake()
    {
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            gameObject.SetActive(true);
        else
            gameObject.SetActive(false);
    }
}
