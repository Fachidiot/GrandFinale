using UnityEngine;
using Newtonsoft.Json.Linq;

public class PlayerController : MonoBehaviour
{
    public float speed = 5.0f;

    private Vector3 lastPosition;
    private float updateInterval = 0.1f; // Send updates 10 times per second
    private float timeSinceLastUpdate = 0f;

    private float lastHorizontal = 0f;
    private float lastVertical = 0f;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Client-side prediction
        Vector3 direction = new Vector3(horizontal, 0, vertical);
        transform.Translate(direction * speed * Time.deltaTime);

        timeSinceLastUpdate += Time.deltaTime;
        if (timeSinceLastUpdate > updateInterval)
        {
            // Send input to server if it has changed
            if (Mathf.Abs(horizontal - lastHorizontal) > 0.01f || Mathf.Abs(vertical - lastVertical) > 0.01f)
            {
                SendInputUpdate(horizontal, vertical);
                lastHorizontal = horizontal;
                lastVertical = vertical;
            }
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
        inputUpdate["input"] = input;

        NetworkManager.Instance.SendMessageToServer(inputUpdate.ToString());
    }
}
