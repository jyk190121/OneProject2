using UnityEngine;
using UnityEngine.EventSystems;

public class BulletSpawner : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("설정")]
    //[SerializeField] private GameObject bulletPrefab;   // 생성할 총알 프리팹
    [SerializeField] private Transform spawnPoint;      // 총알이 나올 위치 (입구)
    [SerializeField] private float bulletSpeed = 10f;   // 총알 속도
    [SerializeField] private float fireRate = 0.2f;     // 총알 속도
    public GameObject player;                           // 플레이어
    JoystickPlayer joysticPlayer;

    private bool isPressed = false; // 버튼이 눌려있는지 확인
    private float nextFireTime = 0f; // 다음 발사 가능 시간

    void Awake()
    {
        if (player != null)
        {
            joysticPlayer = player.GetComponent<JoystickPlayer>();
        }
    }

    void Update()
    {
        if (BattleManager.Instance.isStarting)
        {
            return;
        }

        // 버튼이 눌려있고, 다음 발사 시간이 되었을 때
        if (isPressed && Time.time >= nextFireTime)
        {
            FireBullet();
            nextFireTime = Time.time + fireRate; // 다음 발사 시간 갱신
        }
    }

    // 버튼을 눌렀을 때 호출 (인터페이스 구현)
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;

        joysticPlayer.canRotate = false;
    }

    // 버튼에서 손을 뗐을 때 호출 (인터페이스 구현)
    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;

        joysticPlayer.canRotate = true;
    }
    // 버튼과 연결할 함수
    public void FireBullet()
    {
        if (BulletPoolManager.Instance == null) return;

        GameObject bullet = BulletPoolManager.Instance.GetBullet();

        bullet.transform.position = spawnPoint.position;
        Quaternion bulletFix = Quaternion.Euler(90f, 0, 0f);
        bullet.transform.rotation = spawnPoint.rotation * bulletFix;

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 중요: 풀링된 총알은 이전의 속도가 남아있을 수 있으므로 초기화 후 부여
            rb.linearVelocity = Vector3.zero;
            rb.linearVelocity = spawnPoint.forward * bulletSpeed;
        }

        //if (bulletPrefab == null)
        //{
        //    Debug.LogError("총알 프리팹이 할당되지 않았습니다!");
        //    return;
        //}

        //// 1. 총알 생성 (위치와 회전값을 spawnPoint에 맞춤)
        ////GameObject bullet = Instantiate(bulletPrefab, spawnPoint.position, spawnPoint.rotation);
        
        //Quaternion bulletFix = Quaternion.Euler(90f, 0, 0f);

        //// 최종 회전 = 스폰 지점 회전 상태에서 내부적으로 90도 돌림
        //GameObject bullet = Instantiate(bulletPrefab, spawnPoint.position, spawnPoint.rotation * bulletFix);

        //// 2. 총알 날아가게 하기 (Rigidbody가 있는 경우)
        //Rigidbody rb = bullet.GetComponent<Rigidbody>();
        //if (rb != null)
        //{
        //    // spawnPoint의 정면(forward) 방향으로 물리적인 힘을 가함
        //    rb.linearVelocity = spawnPoint.forward * bulletSpeed;
        //    //rb.AddForce(spawnPoint.forward * bulletSpeed * Time.fixedDeltaTime, ForceMode.VelocityChange);
        //}
        //else
        //{
        //    // Rigidbody가 없다면 총알 자체 스크립트에서 이동 로직이 있어야 합니다.
        //    Debug.LogWarning("총알에 Rigidbody가 없어 물리 이동이 적용되지 않습니다.");
        //}
    }
}