using UnityEngine;
using Newtonsoft.Json.Linq;

[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    public float speed = 5.0f;

    private Animator animator;
    private float updateInterval = 0.05f; // Send updates 20 times per second (to match server tick)
    private float timeSinceLastUpdate = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // This movement is client-side prediction. The server will have the final say.
        Vector3 direction = new Vector3(horizontal, 0, vertical);
        transform.Translate(direction * speed * Time.deltaTime);

        timeSinceLastUpdate += Time.deltaTime;
        if (timeSinceLastUpdate >= updateInterval)
        {
            SendInputUpdate(horizontal, vertical);
            timeSinceLastUpdate = 0f;
        }
    }

    void SendInputUpdate(float horizontal, float vertical)
    {
        JObject inputUpdate = new JObject();
        inputUpdate["type"] = "player_input";
        
        JObject input = new JObject();
        input["h"] = horizontal;
        input["v"] = vertical;
        // Send animator parameters to the server
        // Ensure your Animator has "Forward" and "Strafe" float parameters.
        input["anim_forward"] = animator.GetFloat("Forward");
        input["anim_strafe"] = animator.GetFloat("Strafe");

        inputUpdate["input"] = input;

        NetworkManager.Instance.SendMessageToServer(inputUpdate.ToString());
    }
}
