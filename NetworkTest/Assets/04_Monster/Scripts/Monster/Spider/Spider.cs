/* * 이 파일은 github.com/PhilS94의 Unity-Procedural-IK-Wall-Walking-Spider 프로젝트의 일부입니다.
 * Copyright (C) 2020 Philipp Schofield - All Rights Reserved
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Raycasting;

/// <summary>
/// 거미의 물리적인 움직임과 자세를 총괄하는 핵심 스크립트입니다.
/// 표면을 감지하여 달라붙고, 다리 위치에 따라 몸통의 자세를 보정하며, 외부 컨트롤러로부터 이동/회전 명령을 받아 실행합니다.
/// </summary>
[DefaultExecutionOrder(0)] // 이 스크립트를 제어하는 다른 컨트롤러는 실행 순서를 -1로 설정해야 합니다.
public class Spider : MonoBehaviour
{

    private Rigidbody rb;

    [Header("디버그")]
    public bool showDebug;

    [Header("움직임")]
    [Range(1, 5)]
    public float turnSpeed; // 회전 속도
    [Range(0.001f, 1)]
    public float walkDrag; // 이동 시의 관성 저항

    [Header("지면 감지")]
    public CapsuleCollider capsuleCollider;
    [Range(1, 10)]
    public float gravityMultiplier; // 중력 배수
    [Range(1, 10)]
    public float groundNormalAdjustSpeed; // 바닥 표면에 맞춰 몸을 기울이는 속도
    [Range(1, 10)]
    public float forwardNormalAdjustSpeed; // 벽 표면에 맞춰 몸을 기울이는 속도
    public LayerMask walkableLayer; // 걸을 수 있는 표면의 레이어
    [Range(0, 1)]
    public float gravityOffDistance; // 이 거리 이내로 지면에 붙으면 중력을 끔

    [Header("IK 다리")]
    public Transform body; // 몸통 Transform
    public IKChain[] legs; // 모든 IK 다리 배열

    [Header("몸통 높이 오프셋")]
    public float bodyOffsetHeight;

    [Header("다리 중심점 보정")]
    public bool legCentroidAdjustment; // 다리들의 중심점에 맞춰 몸통 위치를 보정할지 여부
    [Range(0, 100)]
    public float legCentroidSpeed; // 중심점으로 이동하는 속도
    [Range(0, 1)]
    public float legCentroidNormalWeight; // 중심점의 수직(Normal) 방향 보정 가중치
    [Range(0, 1)]
    public float legCentroidTangentWeight; // 중심점의 수평(Tangent) 방향 보정 가중치

    [Header("다리 법선 벡터 보정")]
    public bool legNormalAdjustment; // 다리들이 딛고 있는 평면에 맞춰 몸통을 기울일지 여부
    [Range(0, 100)]
    public float legNormalSpeed; // 평면에 맞춰 기울어지는 속도
    [Range(0, 1)]
    public float legNormalWeight; // 기울임 보정 가중치

    private Vector3 bodyY;
    private Vector3 bodyZ;

    [Header("호흡 효과")]
    public bool breathing; // 몸통이 숨 쉬는 효과를 줄지 여부
    [Range(0.01f, 20)]
    public float breathePeriod; // 숨 쉬는 주기
    [Range(0, 1)]
    public float breatheMagnitude; // 숨 쉬는 폭

    [Header("레이캐스트 조정")]
    [Range(0.0f, 1.0f)]
    public float forwardRayLength; // 전방 감지 레이 길이
    [Range(0.0f, 1.0f)]
    public float downRayLength; // 하단 감지 레이 길이
    [Range(0.1f, 1.0f)]
    public float forwardRaySize = 0.66f; // 전방 감지 레이의 구체(Sphere) 크기
    [Range(0.1f, 1.0f)]
    public float downRaySize = 0.9f; // 하단 감지 레이의 구체(Sphere) 크기
    private float downRayRadius;

    private Vector3 currentVelocity;
    private bool isMoving = true;
    private bool groundCheckOn = true;

    private Vector3 lastNormal;
    private Vector3 bodyDefaultCentroid;
    private Vector3 bodyCentroid;

    private SphereCast downRay, forwardRay;
    private RaycastHit hitInfo;

    private enum RayType { None, ForwardRay, DownRay };
    private struct groundInfo
    {
        public bool isGrounded;
        public Vector3 groundNormal;
        public float distanceToGround;
        public RayType rayType;

        public groundInfo(bool isGrd, Vector3 normal, float dist, RayType m_rayType)
        {
            isGrounded = isGrd;
            groundNormal = normal;
            distanceToGround = dist;
            rayType = m_rayType;
        }
    }

    private groundInfo grdInfo;

    /// <summary>
    /// 컴포넌트 참조를 초기화하고, 스케일 경고 및 레이캐스트 변수들을 설정합니다.
    /// </summary>
    private void Awake()
    {
        // 스케일이 균일한지 확인합니다. 그렇지 않으면 lossyScale이 정확하지 않을 수 있습니다.
        float x = transform.localScale.x; float y = transform.localScale.y; float z = transform.localScale.z;
        if (Mathf.Abs(x - y) > float.Epsilon || Mathf.Abs(x - z) > float.Epsilon || Mathf.Abs(y - z) > float.Epsilon)
        {
            Debug.LogWarning("거미의 XYZ 스케일이 동일하지 않습니다. 스케일은 Y축 기준으로 계산되므로 여러 값에 영향을 미칩니다.");
        }

        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 두 개의 구체(Sphere) 레이캐스트를 초기화합니다.
        downRayRadius = downRaySize * getColliderRadius();
        float forwardRayRadius = forwardRaySize * getColliderRadius();
        downRay = new SphereCast(transform.position, -transform.up, downRayLength * getColliderLength(), downRayRadius, transform, transform);
        forwardRay = new SphereCast(transform.position, transform.forward, forwardRayLength * getColliderLength(), forwardRayRadius, transform, transform);

        // 몸통의 로컬 Y/Z축 방향 및 기본 중심점을 초기화합니다.
        bodyY = body.transform.InverseTransformDirection(transform.up);
        bodyZ = body.transform.InverseTransformDirection(transform.forward);
        bodyCentroid = body.transform.position + getScale() * bodyOffsetHeight * transform.up;
        bodyDefaultCentroid = transform.InverseTransformPoint(bodyCentroid);
    }

    /// <summary>
    /// 물리 업데이트 루프. 지면을 체크하고, 표면에 맞춰 몸을 회전시키며, 중력을 적용합니다.
    /// </summary>
    void FixedUpdate()
    {
        //** 지면 체크 **//
        grdInfo = GroundCheck();

        //** 표면 법선 벡터에 맞춰 회전 **// 
        float normalAdjustSpeed = (grdInfo.rayType == RayType.ForwardRay) ? forwardNormalAdjustSpeed : groundNormalAdjustSpeed;

        Vector3 slerpNormal = Vector3.Slerp(transform.up, grdInfo.groundNormal, 0.02f * normalAdjustSpeed);
        Quaternion goalrotation = getLookRotation(Vector3.ProjectOnPlane(transform.right, slerpNormal), slerpNormal);

        // 나중에 참조할 수 있도록 마지막 법선 벡터를 저장합니다.
        lastNormal = transform.up;

        // 거미에 회전을 적용합니다.
        if (Quaternion.Angle(transform.rotation, goalrotation) > Mathf.Epsilon)
        {
            rb.MoveRotation(goalrotation);
        }

        // 지면과 충분히 가깝지 않을 때만 중력을 적용합니다.
        if (grdInfo.distanceToGround > getGravityOffDistance())
        {
            // Lerp 중인 법선이 아닌, 실제 감지된 지면 법선을 사용해야 합니다.
            rb.AddForce(-grdInfo.groundNormal * gravityMultiplier * 0.0981f * getScale());
        }
    }

    /// <summary>
    /// 매 프레임 업데이트 루프. 디버그 정보 표시, 다리 위치에 따른 몸통 자세 보정, 호흡 효과 등을 처리합니다.
    /// </summary>
    void Update()
    {
        //** 디버그 정보 표시 **//
        if (showDebug) drawDebug();

        Vector3 Y = body.TransformDirection(bodyY);

        if (legCentroidAdjustment) bodyCentroid = Vector3.Lerp(bodyCentroid, getLegsCentroid(), Time.deltaTime * legCentroidSpeed);
        else bodyCentroid = getDefaultCentroid();

        body.transform.position = bodyCentroid;

        if (legNormalAdjustment)
        {
            Vector3 newNormal = GetLegsPlaneNormal();
            Vector3 X = transform.right;
            float angleX = Vector3.SignedAngle(Vector3.ProjectOnPlane(Y, X), Vector3.ProjectOnPlane(newNormal, X), X);
            angleX = Mathf.LerpAngle(0, angleX, Time.deltaTime * legNormalSpeed);
            body.transform.rotation = Quaternion.AngleAxis(angleX, X) * body.transform.rotation;
            Vector3 Z = body.TransformDirection(bodyZ);
            float angleZ = Vector3.SignedAngle(Y, Vector3.ProjectOnPlane(newNormal, Z), Z);
            angleZ = Mathf.LerpAngle(0, angleZ, Time.deltaTime * legNormalSpeed);
            body.transform.rotation = Quaternion.AngleAxis(angleZ, Z) * body.transform.rotation;
        }

        if (breathing)
        {
            float t = (Time.time * 2 * Mathf.PI / breathePeriod) % (2 * Mathf.PI);
            float amplitude = breatheMagnitude * getColliderRadius();
            Vector3 direction = body.TransformDirection(bodyY);

            body.transform.position = bodyCentroid + amplitude * (Mathf.Sin(t) + 1f) * direction;
        }

        // transform이 변경되었는지 여부로 움직임 상태를 판단합니다.
        if (transform.hasChanged)
        {
            isMoving = true;
            transform.hasChanged = false;
        }
        else isMoving = false;
    }

    /// <summary>
    /// 실제 이동을 처리하는 내부 함수입니다. 입력된 방향과 속도에 따라 Rigidbody의 위치를 변경합니다.
    /// </summary>
    private void move(Vector3 direction, float speed)
    {
        float magnitude = direction.magnitude;
        if (magnitude > 1)
        {
            direction = direction.normalized;
            magnitude = 1f;
        }

        if (direction != Vector3.zero)
        {
            // <<<< 핵심 수정 1: 아래 directionDamp 계산을 반드시 삭제 또는 주석 처리해야 합니다. >>>>
            // float directionDamp = Mathf.Pow(Mathf.Clamp(Vector3.Dot(direction.normalized, Vector3.forward), 0, 1), 2);

            // <<<< 핵심 수정 2: directionDamp를 사용하지 않고 speed로만 거리를 계산합니다. >>>>
            // 이전에 0.0004f 였던 계수는 원하는 속도에 맞춰 조절합니다. (예: 0.001f ~ 0.02f)
            float distance = speed * magnitude * Time.fixedDeltaTime;

            distance = Mathf.Clamp(distance, 0, 0.99f * downRayRadius);
            direction = distance * (direction / magnitude);
        }

        currentVelocity = Vector3.Slerp(currentVelocity, direction, 1f - walkDrag);

        rb.MovePosition(transform.position + transform.TransformDirection(currentVelocity));
    }

    /// <summary>
    /// 실제 회전을 처리하는 함수입니다. AI가 보내준 로컬 방향을 기준으로 월드 방향을 계산하여 회전합니다.
    /// </summary>
    public void turn(Vector3 goalForward)
    {
        Vector3 worldGoalForward = transform.TransformDirection(goalForward);
        worldGoalForward = Vector3.ProjectOnPlane(worldGoalForward, transform.up).normalized;

        if (worldGoalForward == Vector3.zero || Vector3.Angle(worldGoalForward, transform.forward) < Mathf.Epsilon)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(worldGoalForward, transform.up), turnSpeed);
        rb.MoveRotation(targetRotation);
    }

    /// <summary>
    /// 외부(AI)에서 호출하는 '걷기' 명령 함수입니다.
    /// </summary>
    public void walk(Vector3 direction, float speed)
    {


        if (direction.magnitude < Mathf.Epsilon) return;
        move(direction, speed); // 이제 walkSpeed 대신 외부에서 받은 speed 값을 사용합니다.
    }

    /// <summary>
    /// 외부(AI)에서 호출하는 '뛰기' 명령 함수입니다.
    /// </summary>
    public void run(Vector3 direction, float speed)
    {
        if (direction.magnitude < Mathf.Epsilon) return;
        move(direction, speed); // 이제 runSpeed 대신 외부에서 받은 speed 값을 사용합니다.
    }

    /// <summary>
    /// 지면을 체크하는 함수입니다. 전방 및 하단 레이캐스트를 사용해 지면 정보를 반환합니다.
    /// </summary>
    private groundInfo GroundCheck()
    {
        if (groundCheckOn)
        {
            if (forwardRay.castRay(out hitInfo, walkableLayer))
            {
                return new groundInfo(true, hitInfo.normal.normalized, Vector3.Distance(transform.TransformPoint(capsuleCollider.center), hitInfo.point) - getColliderRadius(), RayType.ForwardRay);
            }

            if (downRay.castRay(out hitInfo, walkableLayer))
            {
                return new groundInfo(true, hitInfo.normal.normalized, Vector3.Distance(transform.TransformPoint(capsuleCollider.center), hitInfo.point) - getColliderRadius(), RayType.DownRay);
            }
        }
        return new groundInfo(false, Vector3.up, float.PositiveInfinity, RayType.None);
    }

    /// <summary>
    /// 지정된 '오른쪽'과 '위쪽' 벡터를 기준으로 목표 회전값(Quaternion)을 계산합니다.
    /// </summary>
    private Quaternion getLookRotation(Vector3 right, Vector3 up)
    {
        if (up == Vector3.zero || right == Vector3.zero) return Quaternion.identity;
        // 벡터들이 평행하면 identity를 반환합니다.
        float angle = Vector3.Angle(right, up);
        if (angle == 0 || angle == 180) return Quaternion.identity;
        Vector3 forward = Vector3.Cross(right, up);
        return Quaternion.LookRotation(forward, up);
    }

    /// <summary>
    /// 모든 다리 끝(End Effector)의 중심점을 계산하여 몸통의 위치를 보정합니다.
    /// </summary>
    private Vector3 getLegsCentroid()
    {
        if (legs == null || legs.Length == 0)
        {
            Debug.LogError("다리가 할당되지 않아 중심점을 계산할 수 없습니다.");
            return body.transform.position;
        }
        Vector3 defaultCentroid = getDefaultCentroid();
        // 다리 위치의 중심점을 계산합니다.
        Vector3 newCentroid = Vector3.zero;
        float k = 0;
        for (int i = 0; i < legs.Length; i++)
        {
            newCentroid += legs[i].getEndEffector().position;
            k++;
        }
        newCentroid = newCentroid / k;

        // 계산된 중심점을 오프셋합니다.
        Vector3 offset = Vector3.Project(defaultCentroid - getColliderBottomPoint(), transform.up);
        newCentroid += offset;

        // 필요한 수직 및 수평 이동량을 계산합니다.
        Vector3 normalPart = Vector3.Project(newCentroid - defaultCentroid, transform.up);
        Vector3 tangentPart = Vector3.ProjectOnPlane(newCentroid - defaultCentroid, transform.up);

        return defaultCentroid + Vector3.Lerp(Vector3.zero, normalPart, legCentroidNormalWeight) + Vector3.Lerp(Vector3.zero, tangentPart, legCentroidTangentWeight);
    }

    /// <summary>
    /// 모든 다리 끝이 형성하는 가상의 평면의 법선 벡터를 계산하여 몸통의 기울기를 보정합니다.
    /// </summary>
    private Vector3 GetLegsPlaneNormal()
    {
        if (legs == null)
        {
            Debug.LogError("다리가 할당되지 않아 법선 벡터를 계산할 수 없습니다.");
            return transform.up;
        }

        if (legNormalWeight <= 0f) return transform.up;

        Vector3 newNormal = transform.up;
        Vector3 toEnd;
        Vector3 currentTangent;

        for (int i = 0; i < legs.Length; i++)
        {
            toEnd = legs[i].getEndEffector().position - transform.position;
            currentTangent = Vector3.ProjectOnPlane(toEnd, transform.up);

            if (currentTangent == Vector3.zero) continue;

            newNormal = Quaternion.Lerp(Quaternion.identity, Quaternion.FromToRotation(currentTangent, toEnd), legNormalWeight) * newNormal;
        }
        return newNormal;
    }


    //** Getters (정보 반환 함수들) **//

    /// <summary> 현재 Y축 스케일 값을 반환합니다. </summary>
    public float getScale()
    {
        return transform.lossyScale.y;
    }

    /// <summary> 현재 움직이는 중인지 여부를 반환합니다. </summary>
    public bool getIsMoving()
    {
        return isMoving;
    }

    /// <summary> 초당 현재 속도를 반환합니다. </summary>
    public Vector3 getCurrentVelocityPerSecond()
    {
        return currentVelocity / Time.fixedDeltaTime;
    }

    /// <summary> 물리 프레임당 현재 속도를 반환합니다. </summary>
    public Vector3 getCurrentVelocityPerFixedFrame()
    {
        return currentVelocity;
    }

    /// <summary> 현재 감지된 지면의 법선 벡터를 반환합니다. </summary>
    public Vector3 getGroundNormal()
    {
        return grdInfo.groundNormal;
    }

    /// <summary> 마지막 프레임의 위쪽 방향 벡터를 반환합니다. </summary>
    public Vector3 getLastNormal()
    {
        return lastNormal;
    }

    /// <summary> 스케일이 적용된 콜라이더의 반지름을 반환합니다. </summary>
    public float getColliderRadius()
    {
        return getScale() * capsuleCollider.radius;
    }

    /// <summary> 스케일이 적용되지 않은 원본 콜라이더의 반지름을 반환합니다. </summary>
    public float getNonScaledColliderRadius()
    {
        return capsuleCollider.radius;
    }

    /// <summary> 스케일이 적용된 콜라이더의 높이를 반환합니다. </summary>
    public float getColliderLength()
    {
        return getScale() * capsuleCollider.height;
    }

    /// <summary> 월드 좌표계 기준 콜라이더의 중심 위치를 반환합니다. </summary>
    public Vector3 getColliderCenter()
    {
        return transform.TransformPoint(capsuleCollider.center);
    }

    /// <summary> 월드 좌표계 기준 콜라이더의 최하단 위치를 반환합니다. </summary>
    public Vector3 getColliderBottomPoint()
    {
        return transform.TransformPoint(capsuleCollider.center - capsuleCollider.radius * new Vector3(0, 1, 0));
    }

    /// <summary> 월드 좌표계 기준 몸통의 기본 중심 위치를 반환합니다. </summary>
    public Vector3 getDefaultCentroid()
    {
        return transform.TransformPoint(bodyDefaultCentroid);
    }

    /// <summary> 중력이 꺼지는 거리 값을 반환합니다. </summary>
    public float getGravityOffDistance()
    {
        return gravityOffDistance * getColliderRadius();
    }

    //** Setters (값 설정 함수들) **//

    /// <summary> 지면 체크 기능의 활성화 여부를 설정합니다. </summary>
    public void setGroundcheck(bool b)
    {
        groundCheckOn = b;
    }

    //** Debug Methods (디버그용 함수들) **//

    /// <summary> 씬(Scene) 뷰에 디버그 정보를 그립니다. </summary>
    private void drawDebug()
    {
        // 두 개의 구체 레이캐스트를 그립니다.
        downRay.draw(Color.green);
        forwardRay.draw(Color.blue);

        // 중력이 꺼지는 거리를 그립니다.
        Vector3 borderpoint = getColliderBottomPoint();
        Debug.DrawLine(borderpoint, borderpoint + getGravityOffDistance() * -transform.up, Color.magenta);

        // 현재 위쪽(transform.up) 방향과 몸통의 Y축 방향을 그립니다.
        Debug.DrawLine(transform.position, transform.position + 2f * getColliderRadius() * transform.up, new Color(1, 0.5f, 0, 1));
        Debug.DrawLine(transform.position, transform.position + 2f * getColliderRadius() * body.TransformDirection(bodyY), Color.blue);

        // 중심점들을 그립니다.
        DebugShapes.DrawPoint(getDefaultCentroid(), Color.magenta, 0.1f);
        DebugShapes.DrawPoint(getLegsCentroid(), Color.red, 0.1f);
        DebugShapes.DrawPoint(getColliderBottomPoint(), Color.cyan, 0.1f);
    }

#if UNITY_EDITOR
    /// <summary> 에디터에서 선택했을 때 기즈모를 그립니다. (실행 중이 아닐 때도 표시) </summary>
    void OnDrawGizmosSelected()
    {
        if (!showDebug) return;
        if (UnityEditor.EditorApplication.isPlaying) return;
        if (!UnityEditor.Selection.Contains(transform.gameObject)) return;

        Awake();
        drawDebug();
    }
#endif

}