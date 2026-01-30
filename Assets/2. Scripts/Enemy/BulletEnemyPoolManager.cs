using UnityEngine;
using UnityEngine.Pool;

public class BulletEnemyPoolManager : MonoBehaviour
{
    public static BulletEnemyPoolManager Instance;

    [SerializeField] private GameObject bulletPrefab;
    private IObjectPool<GameObject> pool;

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
}