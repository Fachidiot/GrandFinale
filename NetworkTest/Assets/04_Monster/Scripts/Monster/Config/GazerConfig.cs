// GazerConfig.cs (수정된 코드)
using UnityEngine;

[CreateAssetMenu(fileName = "NewGazerConfig", menuName = "Monster/Gazer Config")]
public class GazerConfig : MonsterConfig
{
    [Header("Gazer (Beam Attack)")]
    [Tooltip("빔 공격을 시도할 최대 사거리")]
    public float beamRange = 15f;
    [Tooltip("빔 캐스팅 시간 (Cast3Start -> Cast3End)")]
    public float beamCastTime = 3.5f;
    [Tooltip("빔 공격의 쿨다운")]
    public float beamCooldown = 20f;
    [Tooltip("Cast3Start 애니메이션 후, 빔이 실제로 발사될 때까지의 딜레이")]
    public float beamFireDelay = 0.3f;
    [Tooltip("빔 프리팹 (액션빔)")]
    public GameObject beamPrefab;


    [Header("Gazer (Evasion)")]
    [Tooltip("이 거리보다 가까우면 회피(Strafe)를 시도합니다.")]
    public float strafeRange = 8f;
    [Tooltip("회피(Strafe) 기동의 쿨다운")]
    public float strafeCooldown = 5f;
    [Tooltip("회피 애니메이션의 지속 시간")]
    public float strafeDuration = 1.0f;
}