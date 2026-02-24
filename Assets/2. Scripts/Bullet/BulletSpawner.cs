using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BulletSpawner : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("설정")]
    //[SerializeField] private GameObject bulletPrefab; // 생성할 총알 프리팹
    [SerializeField] private Transform spawnPoint;      // 총알이 나올 위치 (입구)
    [SerializeField] private float bulletSpeed = 10f;   // 총알 속도
    [SerializeField] private float fireRate = 0.1f;     // 발사 속도
    public GameObject player;                           // 플레이어
    JoystickPlayer joysticPlayer;

    private bool isPressed = false;                     // 버튼이 눌려있는지 확인
    private float nextFireTime = 0f;                    // 다음 발사 가능 시간

    //void Awake()
    //{
    //    //player = BattleManager.Instance.joystickPlayers[0].GetComponent<GameObject>();

    //    if (player != null)
    //    {
    //        joysticPlayer = player.GetComponent<JoystickPlayer>();
    //    }
    //}

    // BattleManager에서 호출해줄 함수
    public void SetTargetPlayer(JoystickPlayer target)
    {
        if (target == null) return;

        joysticPlayer = target;
        player = target.gameObject;

        if (spawnPoint == null) spawnPoint = target.networkSpawnPoint;
    }

    void FixedUpdate()
    {
        if (BattleManager.Instance.isStarting) return;

        // 객체가 파괴되었거나(null) 죽었는지 체크
        if (joysticPlayer == null || !joysticPlayer.gameObject.activeInHierarchy || joysticPlayer.GetDeadStatus())
        {
            // 타겟이 없으면 매니저에게 즉시 새 타겟 요청
            BattleManager.Instance.UpdateSpawnerToAlivePlayer();

            // 그래도 없으면 (전원 사망) 작동 중지
            if (joysticPlayer == null) return;
        }

        // 1. UI 버튼(isPressed)
        // 2. 키보드 X 또는 Space
        // 3. 게임패드의 남쪽 버튼 (A/X)
        bool isFiring = isPressed ||
                        //Input.GetKey(Key.X) ||
                        Input.GetKey(Key.Space) ||
                        Input.GetGamepadButton();

        // 발사 중일 때 회전 제어 (발사 중에는 회전 막기)
        if (isFiring) joysticPlayer.canRotate = false;
        // 버튼에서 손 뗐을 때만 다시 회전 허용
        else if (!isPressed) joysticPlayer.canRotate = true; 

        if (isFiring && Time.time >= nextFireTime)
        {
            FireBullet();
            nextFireTime = Time.time + fireRate;
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

        if (joysticPlayer == null || joysticPlayer.networkSpawnPoint == null)
        {
            //// 타겟이 죽었거나 없으면 새로 갱신 요청
            //BattleManager.Instance.UpdateSpawnerToAlivePlayer();
            return;
        }

        Transform currentSpawnPoint = joysticPlayer.networkSpawnPoint;

        joysticPlayer.animController.PlayAttack();

        joysticPlayer.ApplyKickback();

        // (싱글/멀티 공통)
        ExecuteLocalFire(currentSpawnPoint);

        // 멀티에서만
        if(NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            //joysticPlayer.RequestFireServerRpc(spawnPoint.position, spawnPoint.rotation, joysticPlayer.playerData.ATT);
            joysticPlayer.RequestFireServerRpc(currentSpawnPoint.position, currentSpawnPoint.rotation);
        }
    }

    private void ExecuteLocalFire(Transform targetSpawn)
    {
        if (BattleManager.Instance == null)
        {
            print("BattleManager가 아직 준비되지 않았습니다.");
            return;
        }

        //GameObject bullet = BulletPoolManager.Instance.GetBullet();
        GameObject bullet = BulletPoolManager.Instance.GetBullet(targetSpawn.position, targetSpawn.rotation);

        //Bullet bulletAtt = bullet.GetComponent<Bullet>();
        //bulletAtt.SetDamage(joysticPlayer.playerData.ATT);
        //bullet.transform.position = targetSpawn.position;
        //Quaternion bulletFix = Quaternion.Euler(90f, 0, 0f);
        //bullet.transform.rotation = targetSpawn.rotation * bulletFix;

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 중요: 풀링된 총알은 이전의 속도가 남아있을 수 있으므로 초기화 후 부여
            rb.linearVelocity = Vector3.zero;
            float speed = joysticPlayer.playerData.ATTSPEED != 0 ? joysticPlayer.playerData.ATTSPEED : bulletSpeed;
            rb.linearVelocity = targetSpawn.forward * speed;

            //if (joysticPlayer.playerData.ATTSPEED != 0)
            //{
            //    rb.linearVelocity = spawnPoint.forward * joysticPlayer.playerData.ATTSPEED;
            //}
            //else
            //{
            //    rb.linearVelocity = spawnPoint.forward * bulletSpeed;
            //}
        }
        if (bullet.TryGetComponent<Bullet>(out var bulletScript))
        {
            bulletScript.SetDamage(joysticPlayer.playerData.ATT);
        }

    }
}