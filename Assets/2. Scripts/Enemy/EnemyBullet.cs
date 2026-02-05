using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class EnemyBullet : MonoBehaviour
{
    //[SerializeField] private float bulletSpeed = 30f;   // 총알 속도
    //[SerializeField] private float fireRate = 0.2f;     // 발사 속도
    [SerializeField] private float lifeTime = 3f;
    //public GameObject bulletEffect;
    public GameObject wallEffect;

    private IObjectPool<GameObject> targetPool;
    private Rigidbody rb;
    //public enum OwnerType { Player, Enemy }
    //public OwnerType owner; // 에너미가 쏘면 Enemy로 설정

    float damage;

    void Awake() => rb = GetComponent<Rigidbody>();

    public void SetPool(IObjectPool<GameObject> pool) => targetPool = pool;

    void OnEnable()
    {
        // 활성화될 때마다 n초 뒤에 자동으로 풀로 복귀하는 코루틴 시작
        StartCoroutine(DeactivateAfterTime());
    }

    IEnumerator DeactivateAfterTime()
    {
        yield return new WaitForSeconds(lifeTime);
        ReturnToPool();
    }

    public void SetDamage(float att)
    {
        this.damage = att;
    }

    private void OnCollisionEnter(Collision collision)
    {
        int targetLayer = collision.gameObject.layer;
        bool isWall = targetLayer == LayerMask.NameToLayer("Wall");
        bool isDWall = targetLayer == LayerMask.NameToLayer("D_Wall");

        //bool isTarget = false;
        //if (owner == OwnerType.Player)
        //    isTarget = targetLayer == LayerMask.NameToLayer("Enemy");
        //else
        //    isTarget = targetLayer == LayerMask.NameToLayer("Player");

        ContactPoint contact = collision.contacts[0];

        if (isWall || isDWall)
        {
            SpawnEffect(contact, "BulletEffect");

            if (isDWall)
            {
                DestroyEffect(contact);
                ABOX Abox = collision.gameObject.GetComponent<ABOX>();
                if (Abox != null)
                {
                    //Destroy(collision.gameObject);
                    Abox.TakeDamage(1f);
                }
            }
            ReturnToPool();
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            SpawnEffect(contact, "PlayerEffect");

            if (collision.gameObject.TryGetComponent<BaseUnit>(out var unit))
            {
                unit.TakeDamage(this.damage);
                //ReturnToPool();
                print($"플레이어 HP : {unit.currentHP}");
            }

            ReturnToPool();
        }
    }

    private void SpawnEffect(ContactPoint contact, string effectName)
    {
        Quaternion rot = Quaternion.LookRotation(contact.normal);
        // 풀에서 가져오기
        GameObject effect = EnemyEffectPooler.Instance.GetEffect(effectName);
        effect.transform.position = contact.point;
        effect.transform.rotation = rot;

        // 파괴가 아닌 '반납'
        EnemyEffectPooler.Instance.StartCoroutine(EnemyEffectPooler.Instance.ReturnEffectAfterTime(effectName, effect, 0.5f));
    }

    private void DestroyEffect(ContactPoint contact)
    {
        Quaternion rot = Quaternion.LookRotation(contact.normal);
        if (wallEffect != null)
        {
            GameObject effect = Instantiate(wallEffect, contact.point, rot);
            Destroy(effect, 0.5f); // 이펙트도 풀링하면 좋지
        }
    }
    private void ReturnToPool()
    {
        if (gameObject.activeSelf)
        {
            StopAllCoroutines();
            rb.linearVelocity = Vector3.zero; // 물리 속도 초기화 필수
            targetPool.Release(gameObject);   // 풀로 반납
        }
    }
}
