using UnityEngine;
public class BaseUnit : MonoBehaviour
{
    // 현재 실시간으로 변하는 스탯
    public float currentHP;
    protected bool isDead = false;

    // 초기화 함수
    protected virtual void InitStats(float maxHP)
    {
        currentHP = maxHP;
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHP -= damage;
        if (currentHP <= 0) Die();
    }

    protected virtual void Die()
    {
        isDead = true;
        gameObject.SetActive(false);
    }
}
