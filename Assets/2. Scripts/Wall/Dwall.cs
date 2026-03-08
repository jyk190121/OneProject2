using Unity.Netcode;
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
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        if (isNetworkActive)
        {
            // [핵심] 멀티플레이 중이라면 '서버'일 때만 Despawn을 시도합니다.
            if (NetworkManager.Singleton.IsServer)
            {
                var netObj = GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn(); // 서버가 호출하면 모든 클라이언트에서 사라집니다.
                }
            }
            // 클라이언트라면 아무것도 하지 않습니다. (서버의 Despawn을 기다림)
        }
        // 싱글플레이라면 즉시 파괴
        else
        {
            Destroy(gameObject);
        }

        // 이펙트는 모든 클라이언트에서 보이게 하고 싶다면?
        // 여기서 생성하거나, 더 정확하게는 OnDestroy() 등에서 처리하는 것이 좋습니다.
        if (destroyEffect != null)
        {
            GameObject effect = Instantiate(destroyEffect, transform.position, transform.rotation);
            Destroy(effect, 1.5f);
        }

        //var netObj = gameObject.GetComponent<NetworkObject>();
        //if (NetworkManager.Singleton.IsServer && netObj.IsSpawned)
        //{
        //    if (destroyEffect != null)
        //    {
        //        // 이전에 만든 이펙트 풀링이나 Instantiate 사용
        //        GameObject effect = Instantiate(destroyEffect, transform.position, transform.rotation);
        //        Destroy(effect, 1.5f);
        //    }

        //    //Destroy(gameObject);
        //    GetComponent<NetworkObject>().Despawn();
        //}
    }
}
