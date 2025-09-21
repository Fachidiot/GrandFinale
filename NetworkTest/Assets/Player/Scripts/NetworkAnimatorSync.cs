using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NetworkAnimatorSync : MonoBehaviour
{
    public bool isMine = false;

    private Animator animator;
    private NetworkManager networkManager;
    private string objectId;

    // --- Send data ---
    private float sendInterval = 0.1f;
    private float nextSendTime;

    // --- Receive and interpolate data ---
    private float targetX = 0f;
    private float targetY = 0f;
    private float smoothingFactor = 10.0f;

    // Animator parameter hashes for efficiency
    private readonly int xHash = Animator.StringToHash("x");
    private readonly int yHash = Animator.StringToHash("y");
    private readonly int walkHash = Animator.StringToHash("walk");
    private readonly int sprintHash = Animator.StringToHash("sprint");
    private readonly int rollHash = Animator.StringToHash("roll");
    private readonly int isGroundedHash = Animator.StringToHash("isGrounded");
    private readonly int crouchHash = Animator.StringToHash("crouch");

    void Awake()
    {
        animator = GetComponent<Animator>();
        animator.enabled = false;
    }

    void Start()
    {
        networkManager = NetworkManager.Instance;
    }

    public void Initialize(string id, bool isOwner)
    {
        this.objectId = id;
        this.isMine = isOwner;
        animator.enabled = true;
    }

    void Update()
    {
        if (isMine)
        {
            if (Time.time >= nextSendTime)
            {
                SendAnimationParameters();
                nextSendTime = Time.time + sendInterval;
            }
        }
        else
        {
            // For remote clients, smoothly update float parameters
            float currentX = animator.GetFloat(xHash);
            float currentY = animator.GetFloat(yHash);
            animator.SetFloat(xHash, Mathf.Lerp(currentX, targetX, Time.deltaTime * smoothingFactor));
            animator.SetFloat(yHash, Mathf.Lerp(currentY, targetY, Time.deltaTime * smoothingFactor));
        }
    }

    private void SendAnimationParameters()
    {
        var animData = new
        {
            type = "anim_sync",
            player_id = this.objectId,
            x = animator.GetFloat(xHash),
            y = animator.GetFloat(yHash),
            walk = animator.GetBool(walkHash),
            sprint = animator.GetBool(sprintHash),
            roll = animator.GetBool(rollHash),
            isGrounded = animator.GetBool(isGroundedHash),
            crouch = animator.GetBool(crouchHash)
        };

        string jsonMessage = Newtonsoft.Json.JsonConvert.SerializeObject(animData);
        networkManager.SendUDPMessage(jsonMessage);
    }

    public void OnAnimationDataReceived(float x, float y, bool walk, bool sprint, bool roll, bool isGrounded, bool crouch)
    {
        if (!isMine)
        {
            // Store target float values for interpolation
            targetX = x;
            targetY = y;

            // Set bool values directly
            animator.SetBool(walkHash, walk);
            animator.SetBool(sprintHash, sprint);
            animator.SetBool(crouchHash, crouch);
            animator.SetBool(isGroundedHash, isGrounded);

            // Handle triggers like roll directly
            if (roll)
            {
                animator.SetTrigger(rollHash);
            }
        }
    }
}