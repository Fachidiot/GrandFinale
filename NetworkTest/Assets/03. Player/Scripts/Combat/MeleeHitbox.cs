using UnityEngine;
using System.Collections.Generic;

public class MeleeHitbox : MonoBehaviour
{
    private int damage;
    private bool isAttack = false;
    [SerializeField] private LayerMask hitLayers;
    private List<GameObject> hitTargets = new List<GameObject>(); // 중복 타격 방지용

    // 공격 시작 시 외부(UnarmedWeapon)에서 호출
    public void EnableHitbox(int dmg)
    {
        damage = dmg;
        hitTargets.Clear(); // 맞은 목록 초기화
        isAttack = true;
    }

    // 공격 종료 시 호출
    public void DisableHitbox()
    {
        isAttack = false;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!isAttack && other.gameObject.layer != hitLayers)
            return;

        // 이미 이번 공격에 맞은 적이면 패스
        if (hitTargets.Contains(other.gameObject)) return;

        // 적(Enemy)인지 태그나 레이어로 확인 (프로젝트 설정에 맞게 수정 필요)
        if (other.gameObject.CompareTag("HitBox"))
        {
            // 데미지 주기 (인터페이스나 컴포넌트 사용)
            // 예: IDamageable target = other.GetComponent<IDamageable>();
            // if (target != null) target.TakeDamage(damage);

            Debug.Log($"{other.gameObject.name}에게 {damage} 데미지!");

            hitTargets.Add(other.gameObject); // 맞은 목록에 추가
        }
        else if (other.gameObject.CompareTag("Sandbag"))
        {
            other.transform.parent.GetComponent<SandbagMachine>().Punching();
        }
    }
}