using UnityEngine;

public abstract class Dwall : MonoBehaviour
{
    [Header("설정")]
    public float maxHP;
    protected float currentHP;
    public GameObject destroyEffect;

    protected virtual void Start()
    {
        currentHP = maxHP;
    }

    // 외부(총알 등)에서 호출할 데미지 함수
    public virtual void TakeDamage(float damage)
    {
        currentHP -= damage;

        if (currentHP <= 0)
        {
            DestroyObject();
        }
    }

    protected virtual void DestroyObject()
    {
        if (destroyEffect != null)
        {
            // 이전에 만든 이펙트 풀링이나 Instantiate 사용
            Instantiate(destroyEffect, transform.position, transform.rotation);
        }

        Destroy(gameObject);
    }
}
