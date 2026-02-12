using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;

public class JoystickPlayer : BaseUnit
{
    //public float speed;
    public VariableJoystick variableJoystick;
    public Rigidbody rb;
    public AnimController animController;
    Animator anim;
    public Transform networkSpawnPoint; // 캐릭터의 총구 위치

    // 회전 제어 변수 (기본값 true)
    public bool canRotate = true;

    public Player playerData; // P1_Data, P2_Data 등을 각각 할당

    //public bool isLocalPlayer = true; // 네트워크 매니저가 스폰할 때 설정해줌

    public override void OnNetworkSpawn()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.RegisterPlayer(this);
            anim = GetComponent<Animator>();
            animController = new AnimController(anim);
        }

        if (IsOwner)
        {
            SetupLocalPlayer();
        }
    }

    private void SetupLocalPlayer()
    {
        // [시네머신 설정] 씬에 있는 CinemachineCamera를 찾아 나를 추적하게 함
        var vcam = GameObject.FindAnyObjectByType<CinemachineCamera>();
        if (vcam != null)
        {
            vcam.Target.TrackingTarget = transform;
            vcam.Target.LookAtTarget = transform;
        }

        //// 조이스틱 및 UI 연결 (BattleManager를 통해 전달받은 것 사용)
        //if (variableJoystick == null) variableJoystick = BattleManager.Instance.joystick;

        // [수정] 직접 매니저의 필드에 접근하여 할당 (Owner가 스스로 가져가는 방식이 더 확실합니다)
        if (BattleManager.Instance != null)
        {
            //this.variableJoystick = BattleManager.Instance.joystick;
            //this.HP_BAR = BattleManager.Instance.playerHP_bar;

            //// 스포너 설정도 여기서 수행
            //if (BattleManager.Instance.bulletSpawner != null)
            //{
            //    BattleManager.Instance.bulletSpawner.SetTargetPlayer(this);
            //}

            if (variableJoystick == null)
            {
                variableJoystick = BattleManager.Instance.joystick;
            }
            if (HP_BAR == null)
            {
                HP_BAR = BattleManager.Instance.playerHP_bar;
            }
        }
    }

    //void Start()
    //{
    //    //anim = GetComponent<Animator>();

    //    //animController = new AnimController(anim);

    //    //BattleManager.Instance.RegisterPlayer(this);

    //    //if (playerData != null)
    //    //{
    //    //    InitStats(playerData.HP);

    //    //    SetDeathEffect(playerData.DIEEFFECT);
    //    //    // playerData.SPEED 등을 사용하여 이동 속도 설정
    //    //}
    //}

    public void FixedUpdate()
    {
        if (!IsOwner) return;
        Vector3 moveDir = new Vector3(variableJoystick.Horizontal, 0, variableJoystick.Vertical);

        if (BattleManager.Instance.isStarting)
        {
            // 혹시 모를 밀림 방지
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Move();

        //// 조이스틱 값 + 키보드(Input.cs) 값을 더합니다.
        //float h = variableJoystick.Horizontal + Input.GetAxis("Horizontal");
        //float v = variableJoystick.Vertical + Input.GetAxis("Vertical");

        //// 방향 벡터 생성 (3D 환경이므로 x와 z축 사용)
        //Vector3 direction = (Vector3.forward * v) + (Vector3.right * h);

        //// 대각선 이동 속도 보정 (길이가 1을 넘지 않도록 normalized)
        //if (direction.sqrMagnitude > 1f)
        //{
        //    direction.Normalize();
        //}

        ////Vector3 direction = Vector3.forward * variableJoystick.Vertical + Vector3.right * variableJoystick.Horizontal;

        ////rb.AddForce(direction * speed * Time.fixedDeltaTime, ForceMode.Impulse);
        //rb.linearVelocity = direction * playerData.MOVESPEED;

        //HandleRotation(direction);

        //// 입력값의 크기가 아주 작을 때(손을 뗐을 때)는 회전하지 않도록 함
        //if (canRotate && direction.sqrMagnitude > 0.01f)
        //{
        //    // 현재 방향을 바라보는 회전값 생성
        //    Quaternion targetRotation = Quaternion.LookRotation(direction);

        //    // 즉시 회전시키거나, Lerp를 사용하여 부드럽게 회전시킬 수 있음
        //    rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 5f);
        //}

        //animController.PlayMove(direction.sqrMagnitude);

        // 회전 처리
        Vector3 lookDir = Vector3.zero;
        if (moveDir.sqrMagnitude > 0.01f) lookDir = moveDir;

        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            // 클라이언트(Owner)가 회전시키면 NetworkTransform이 이를 가로채서 서버로 보냅니다.
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 10f));
        }
    }

    private void Move()
    {
        float h = variableJoystick.Horizontal + Input.GetAxis("Horizontal");
        float v = variableJoystick.Vertical + Input.GetAxis("Vertical");
        Vector3 direction = (Vector3.forward * v) + (Vector3.right * h);

        if (direction.sqrMagnitude > 1f) direction.Normalize();

        rb.linearVelocity = direction * playerData.MOVESPEED;

        HandleRotation(direction);
        animController.PlayMove(direction.sqrMagnitude);
    }

    protected override void Die()
    {
        base.Die(); // 공통 로직(이펙트 생성 등) 실행

        // 플레이어 전용: 매니저에게 게임 오버 알림
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.GameOver();
        }
      
    }
/*    public void Shoot()
    {
        if (IsOwner) return;

        // 내 총구 위치와 방향을 서버로 보냄
        RequestFireServerRpc(networkSpawnPoint.position, networkSpawnPoint.rotation);
    }*/

    private void HandleRotation(Vector3 moveDir)
    {
        if (!IsOwner || !canRotate) return; // 내 캐릭터만 계산

        Vector3 lookDir = Vector3.zero;
        // [A] 게임패드 오른쪽 스틱 입력 확인
        float rh = Input.GetAxis("LookHorizontal");
        float rv = Input.GetAxis("LookVertical");
        Vector3 stickLookDir = (Vector3.forward * rv) + (Vector3.right * rh);

        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            // rb.rotation 대신 transform.rotation을 사용하면 NetworkTransform이 더 잘 감지합니다.
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 10f);
        }

        if (stickLookDir.sqrMagnitude > 0.1f)
        {
            lookDir = stickLookDir;
        }
        // [B] 마우스 클릭 중이거나 특정 조건일 때 마우스 방향 주시
        else if (Input.GetMouseButton(1)) // 오른쪽 마우스 버튼 누를 때만 회전
        {
            Vector3 mouseWorldPos = Input.GetMouseWorldPosition();
            lookDir = (mouseWorldPos - transform.position);
            lookDir.y = 0;
        }
        // [C] 별도의 회전 입력이 없으면 이동 방향을 바라봄 (기존 방식)
        else if (moveDir.sqrMagnitude > 0.01f)
        {
            lookDir = moveDir;
        }

        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 10f);
        }
    }

    // [중요] 서버에 발사를 요청하는 RPC
    [ServerRpc]
    public void RequestFireServerRpc(Vector3 pos, Quaternion rot, float damage)
    {
        // 서버가 모든 클라이언트에게 총알 생성을 알리는 로직 (또는 NetworkObject 풀링 사용)
        // 여기서는 간단하게 모든 클라이언트에서 실행되도록 ClientRpc를 호출할 수 있습니다.
        FireBulletClientRpc(pos, rot, damage);
    }


    [ServerRpc]
    public void RequestFireServerRpc(Vector3 pos, Quaternion rot)
    {
        // [2] 서버가 모든 클라이언트에게 발사하라고 명령 (서버 -> 모든 클라이언트)
        FireBulletClientRpc(pos, rot, playerData.ATT);
    }


    [ClientRpc]
    private void FireBulletClientRpc(Vector3 pos, Quaternion rot, float damage)
    {
        // 실제 총알 생성 로직 (BulletPoolManager가 각자 클라이언트에 있다고 가정)
        if (IsOwner) return; // 본인은 이미 로컬에서 쐈으므로 제외 (선택 사항)

        //// 여기서 상대방 화면에 보일 총알을 생성합니다.
        // ExecuteFire(pos, rot, damage);
        ExecuteLocalFire(pos, rot, damage);
    }

    //public void ExecuteFire(Vector3 pos, Quaternion rot, float damage)
    //{
    //    animController.PlayAttack();
    //    // ... 실제 BulletPool에서 꺼내서 세팅하는 로직 ...
    //}

    void ExecuteLocalFire(Vector3 pos, Quaternion rot, float damage)
    {
        GameObject bullet = BulletPoolManager.Instance.GetBullet();
        bullet.transform.position = pos;

        // 총알 방향 보정 (기존 로직 유지)
        Quaternion bulletFix = Quaternion.Euler(90f, 0, 0f);
        bullet.transform.rotation = rot * bulletFix;

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.SetDamage(damage);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            // forward 방향으로 물리적인 힘 가하기
            rb.linearVelocity = rot * Vector3.forward * (playerData.ATTSPEED > 0 ? playerData.ATTSPEED : 10f);
        }
    }
}