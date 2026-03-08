using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

public class EnemyBullet : MonoBehaviour
{
    //[SerializeField] private float bulletSpeed = 30f;   // 총알 속도
    //[SerializeField] private float fireRate = 0.2f;     // 발사 속도
    [SerializeField] private float lifeTime = 3f;
    //public GameObject bulletEffect;
    public GameObject bulletEffect;

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

    //// 초기 설정 함수 (적이 총알을 생성한 직후 호출)
    //public void Init(float att, float lifeTime)
    //{
    //    this.damage = att;
    //    this.lifeTime = lifeTime;

    //    // 물리 속도 등 초기화가 필요하다면 여기서 수행
    //    if (rb == null) rb = GetComponent<Rigidbody>();
    //    rb.linearVelocity = Vector3.zero;
    //}

    private void OnCollisionEnter(Collision collision)
    {
        int targetLayer = collision.gameObject.layer;
        //bool isWall = targetLayer == LayerMask.NameToLayer("Wall");
        //bool isDWall = targetLayer == LayerMask.NameToLayer("D_Wall");

        //bool isTarget = false;
        //if (owner == OwnerType.Player)
        //    isTarget = targetLayer == LayerMask.NameToLayer("Enemy");
        //else
        //    isTarget = targetLayer == LayerMask.NameToLayer("Player");

        // 1. 충돌 정보 추출
        ContactPoint contact = collision.contacts[0];

        // 2. 어디에 부딪히건 공통 이펙트 생성
        SpawnImpactEffect(contact);

        //if (isWall || isDWall)
        //{
        //    SpawnEffect(contact);

        //    if (isDWall)
        //    {
        //        DestroyEffect(contact);
        //        ABOX Abox = collision.gameObject.GetComponent<ABOX>();
        //        if (Abox != null)
        //        {
        //            //Destroy(collision.gameObject);
        //            Abox.TakeDamage(1f);
        //        }
        //    }
        //    ReturnToPool();
        //}

        //if (collision.gameObject.CompareTag("Player"))
        //{
        //    SpawnEffect(contact);

        //    if (collision.gameObject.TryGetComponent<BaseUnit>(out var unit))
        //    {
        //        unit.TakeDamage(this.damage);
        //        //ReturnToPool();
        //        //print($"플레이어 HP : {unit.currentHP}");
        //    }

        //    ReturnToPool();
        //}

        // 3-1. 파괴 가능한 벽(D_Wall)인 경우
        if (targetLayer == LayerMask.NameToLayer("D_Wall"))
        {
            if (collision.gameObject.TryGetComponent<ABOX>(out var Abox))
            {
                //var netObj = collision.gameObject.GetComponent<NetworkObject>();
                //if (netObj != null && netObj.IsSpawned)
                //{
                //    Abox.TakeDamage(1f);
                //}
                if (Abox != null)
                {
                    Abox.TakeDamage(1f);
                }
            }
        }
        // 3-2. 플레이어인 경우
        else if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent<BaseUnit>(out var unit))
            {
                unit.TakeDamage(this.damage);
            }
        }

        ReturnToPool();
    }

    //private void SpawnEffect(ContactPoint contact)
    //{
    //    Quaternion rot = Quaternion.LookRotation(contact.normal);
    //    // 풀에서 가져오기
    //    GameObject effect = EnemyEffectPooler.Instance.GetEffect(bulletEffect);
    //    effect.transform.position = contact.point;
    //    effect.transform.rotation = rot;

    //    // 파괴가 아닌 '반납'
    //    EnemyEffectPooler.Instance.StartCoroutine(EnemyEffectPooler.Instance.ReturnEffectAfterTime(bulletEffect, effect, 0.5f));
    //}
    private void SpawnImpactEffect(ContactPoint contact)
    {
        if (bulletEffect == null) return;

        Quaternion rot = Quaternion.LookRotation(contact.normal);

        // EnemyEffectPooler를 사용하여 풀링된 이펙트 가져오기
        GameObject effect = EnemyEffectPooler.Instance.GetEffect(bulletEffect);

        if (effect != null)
        {
            effect.transform.position = contact.point;
            effect.transform.rotation = rot;

            // 일정 시간 후 풀로 반환 (기존 Coroutine 활용)
            EnemyEffectPooler.Instance.StartCoroutine(
                EnemyEffectPooler.Instance.ReturnEffectAfterTime(bulletEffect, effect, 0.5f)
            );
        }
    }

    //private void DestroyEffect(ContactPoint contact)
    //{
    //    Quaternion rot = Quaternion.LookRotation(contact.normal);
    //    if (bulletEffect != null)
    //    {
    //        GameObject effect = Instantiate(bulletEffect, contact.point, rot);
    //        Destroy(effect, 0.5f); // 이펙트도 풀링하면 좋지
    //    }
    //}
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
