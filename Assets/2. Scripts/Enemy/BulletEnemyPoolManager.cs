using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletEnemyPoolManager : MonoBehaviour
{
    public static BulletEnemyPoolManager Instance;

    [SerializeField] private GameObject bulletPrefab;
    private IObjectPool<GameObject> pool;

    // 프리팹별로 풀을 관리하는 딕셔너리
    private Dictionary<int, IObjectPool<GameObject>> poolDictionary = new Dictionary<int, IObjectPool<GameObject>>();

    void Awake()
    {
        Instance = this;
        // 풀 생성: (생성 시 로직, 활성화 시 로직, 비활성화 시 로직, 파괴 시 로직, 초기 용량, 최대 용량)
        pool = new ObjectPool<GameObject>(CreateBullet, OnTakeFromPool, OnReturnToPool, OnDestroyBullet, true, 20, 50);
    }

    private GameObject CreateBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab, transform);
        bullet.GetComponent<EnemyBullet>().SetPool(pool); // 총알에게 자신이 돌아갈 풀을 알려줌
        return bullet;
    }

    private void OnTakeFromPool(GameObject bullet) => bullet.SetActive(true);
    private void OnReturnToPool(GameObject bullet) => bullet.SetActive(false);
    private void OnDestroyBullet(GameObject bullet) => Destroy(bullet);

    // 외부(Spawner)에서 총알을 빌려갈 때 쓰는 함수
    public GameObject GetBullet() => pool.Get();

    // 외부에서 "특정 프리팹"을 넣어주면 해당 풀에서 꺼내주는 함수
    public GameObject GetBullet(GameObject prefab)
    {
        int key = prefab.GetInstanceID();

        // 해당 프리팹용 풀이 없다면 새로 생성
        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary.Add(key, new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab, transform),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: true,
                defaultCapacity: 20,
                maxSize: 50
            ));
        }

        GameObject bullet = poolDictionary[key].Get();

        // 중요: 총알에게 자신이 돌아가야 할 풀(해당 프리팹 전용 풀)을 알려줌
        bullet.GetComponent<EnemyBullet>().SetPool(poolDictionary[key]);

        return bullet;
    }
}