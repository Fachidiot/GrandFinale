using UnityEngine;
using Raycasting; // Spider 프로젝트의 Raycasting 네임스페이스 사용

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class SurfaceAligner : MonoBehaviour
{
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    [Header("자세 제어 설정")]
    [Tooltip("체크 해제 시, 전방 레이를 사용하지 않아 벽에 붙지 않습니다.")]
    public bool allowWallClimbing = true;
    [Range(1, 15)]
    public float alignmentSpeed = 5f;
    [Range(1, 15)]
    public float gravityMultiplier = 5f;

    [Header("레이캐스트 설정")]
    public LayerMask walkableLayer;
    private SphereCast downRay, forwardRay;

    public Vector3 SurfaceNormal { get; private set; } = Vector3.up;
    public bool IsGrounded { get; private set; } = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // 레이캐스트 초기화 (기존 Spider.cs에서 가져옴)
        float scale = transform.lossyScale.y;
        float radius = capsuleCollider.radius * scale;
        float length = capsuleCollider.height * scale;

        downRay = new SphereCast(transform.position, -transform.up, length * 0.6f, radius * 0.9f, transform, transform);
        forwardRay = new SphereCast(transform.position, transform.forward, length * 0.6f, radius * 0.6f, transform, transform);
    }

    void FixedUpdate()
    {
        GroundCheck();
        ApplyGravity();
        AlignToSurface();
    }

    private void GroundCheck()
    {
        IsGrounded = false;
        RaycastHit hitInfo;

        if (allowWallClimbing && forwardRay.castRay(out hitInfo, walkableLayer))
        {
            SurfaceNormal = hitInfo.normal.normalized;
            IsGrounded = true;
            return;
        }

        if (downRay.castRay(out hitInfo, walkableLayer))
        {
            SurfaceNormal = hitInfo.normal.normalized;
            IsGrounded = true;
            return;
        }

        SurfaceNormal = Vector3.up;
    }

    private void ApplyGravity()
    {
        // IsGrounded가 false일 때만 (즉, 공중에 떴을 때만) 중력을 적용합니다.
        if (!IsGrounded)
        {
            rb.AddForce(-SurfaceNormal * gravityMultiplier * 9.81f * rb.mass);
        }
    }

    private void AlignToSurface()
    {
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, SurfaceNormal) * rb.rotation;
        Quaternion smoothedRotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * alignmentSpeed);
        rb.MoveRotation(smoothedRotation);
    }
}