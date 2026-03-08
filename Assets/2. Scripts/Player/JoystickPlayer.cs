using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

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

    [Header("Combat Settings")]
    public float kickbackForce = 10f;

    // 초기화 로직을 공통 함수로 분리
    private void InitPlayer()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.RegisterPlayer(this);
            anim = GetComponent<Animator>();
            animController = new AnimController(anim);
            InitStats(playerData.HP);
            SetDeathEffect(playerData.DIEEFFECT);
        }
    }
   

    public override void OnNetworkSpawn()
    {
        InitPlayer();
        if (IsOwner)
        {
            SetupLocalPlayer();
            // 0번 플레이어(호스트)는 왼쪽, 1번 플레이어(클라이언트)는 오른쪽
            if (OwnerClientId == 0) transform.position = new Vector3(-5, 1, 0);
            else transform.position = new Vector3(5, 1, 0);

            rb.useGravity = true; // 씬 시작 시 중력 켜기
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

        // 직접 매니저의 필드에 접근하여 할당 (Owner가 스스로 가져가는 방식이 더 확실합니다)
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

    IEnumerator Start()
    {
        ResetStatus();

        while (BattleManager.Instance == null)
        {
            yield return null;
        }

        BattleManager.Instance.RegisterPlayer(this);

        // 네트워크가 활성화되지 않은 싱글 모드일 때만 직접 초기화
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            InitPlayer();
            SetupLocalPlayer();
            rb.useGravity = true;
        }
    }

    public void FixedUpdate()
    {
        //if (!IsOwner) return;
        if (!HasControlAuthority || GameSceneManager.Instance.SceneName() == "Lobby") return;

        Vector3 moveDir = new Vector3(variableJoystick.Horizontal, 0, variableJoystick.Vertical);

        if (BattleManager.Instance.isStarting)
        {
            // 혹시 모를 밀림 방지
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Move();

        //// 회전 처리
        //Vector3 lookDir = Vector3.zero;
        //if (moveDir.sqrMagnitude > 0.01f) lookDir = moveDir;

        //if (lookDir != Vector3.zero)
        //{
        //    Quaternion targetRotation = Quaternion.LookRotation(lookDir);
        //    rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 10f));
        //}
    }
    public void ResetStatus()
    {
        // BaseUnit에 선언되어 있을 변수들을 초기화합니다.
        // 변수명은 사용자님의 BaseUnit 구조에 맞게 수정하세요.
        isDead = false;

        if (playerData != null)
        {
            currentHP = playerData.HP; // 현재 체력을 데이터상의 최대 체력으로 복구
        }

        // 혹시 리지드바디가 멈춰있을 수 있으니 초기화
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log($"{gameObject.name}의 상태가 초기화되었습니다. (isDead = false)");
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

    //public void Shoot()
    // {
    //     if (IsOwner) return;
    //
    //     // 내 총구 위치와 방향을 서버로 보냄
    //     RequestFireServerRpc(networkSpawnPoint.position, networkSpawnPoint.rotation);
    // }

    private void HandleRotation(Vector3 moveDir)
    {
        //if (!IsOwner || !canRotate) return; // 내 캐릭터만 계산
        if (!HasControlAuthority) return;

        Vector3 lookDir = Vector3.zero;
        // [A] 게임패드 오른쪽 스틱 입력 확인
        float rh = Input.GetAxis("LookHorizontal");
        float rv = Input.GetAxis("LookVertical");
        Vector3 stickLookDir = (Vector3.forward * rv) + (Vector3.right * rh);
        bool isRightStickActive = stickLookDir.sqrMagnitude > 0.1f;
        bool isMouseRightActive = Input.GetMouseButton(1);

        // 2. [강제 회전] 오른쪽 스틱이나 마우스 입력이 있는 경우 (공격 중에도 무조건 회전)
        if (isRightStickActive || isMouseRightActive)
        {
            if (isRightStickActive)
                lookDir = stickLookDir;
            else
                lookDir = (Input.GetMouseWorldPosition() - transform.position);

            lookDir.y = 0;
        }
        // 3. [자동 회전] 오른쪽 입력이 없고, 왼쪽 조이스틱(이동)만 입력된 경우
        else if (moveDir.sqrMagnitude > 0.01f)
        {
            // 공격 중(canRotate == false)이면 왼쪽 조이스틱에 의한 회전은 절대 하지 않음
            if (!canRotate) return;

            lookDir = moveDir;
        }

        //if (stickLookDir.sqrMagnitude > 0.1f)
        //{
        //    lookDir = stickLookDir;
        //}
        //// [B] 마우스 클릭 중이거나 특정 조건일 때 마우스 방향 주시
        //else if (Input.GetMouseButton(1)) // 오른쪽 마우스 버튼 누를 때만 회전
        //{
        //    Vector3 mouseWorldPos = Input.GetMouseWorldPosition();
        //    lookDir = (mouseWorldPos - transform.position);
        //    lookDir.y = 0;
        //}
        //// [C] 별도의 회전 입력이 없으면 이동 방향을 바라봄 (기존 방식)
        //else if (moveDir.sqrMagnitude > 0.01f)
        //{
        //    lookDir = moveDir;
        //}

        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            //rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 10f);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 10f));
        }
    }

    bool HasControlAuthority
    {
        get
        {
            // 네트워크가 연결되지 않은 상태(싱글 모드)라면 조작 가능
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return true;

            // 네트워크 연결 상태라면 IsOwner일 때만 조작 가능
            return IsOwner;
        }
    }

    [ServerRpc]
    public void RequestFireServerRpc(Vector3 pos, Quaternion rot)
    {
        FireBulletClientRpc(pos, rot, playerData.ATT);
    }

    [ClientRpc]
    private void FireBulletClientRpc(Vector3 pos, Quaternion rot, float damage)
    {
        // 씬 전환 직후라면 다른 클라이언트들에서 매니저가 null일 수 있음
        if (BattleManager.Instance == null) return;
        if (IsOwner) return;

        // 여기서 상대방 화면에 보일 총알을 생성합니다.
        ExecuteLocalFire(pos, rot, damage);
    }

    //public void ExecuteFire(Vector3 pos, Quaternion rot, float damage)
    //{
    //    animController.PlayAttack();
    //    // ... 실제 BulletPool에서 꺼내서 세팅하는 로직 ...
    //}

    void ExecuteLocalFire(Vector3 pos, Quaternion rot, float damage)
    {
        //null체크 추가
        if (animController == null || BulletPoolManager.Instance == null)
        {
            print("애니메이션 컨트롤러 또는 불렛 풀 매니저가 준비되지 않았습니다.");
            return;
        }

        animController.PlayAttack();

        GameObject bullet = BulletPoolManager.Instance.GetBullet(pos, rot);

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.SetDamage(damage);
        }
        //bullet.transform.position = pos;

        // 총알 방향 보정 (기존 로직 유지)
        //Quaternion bulletFix = Quaternion.Euler(90f, 0, 0f);
        //bullet.transform.rotation = rot * bulletFix;
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = Vector3.zero;
            // playerData.ATTSPEED가 0일 경우를 대비한 기본값 설정
            float speed = playerData.ATTSPEED > 0 ? playerData.ATTSPEED : 10f;
            bulletRb.linearVelocity = rot * Vector3.forward * speed;
        }

        //Rigidbody rb = bullet.GetComponent<Rigidbody>();
        //if (rb != null)
        //{
        //    rb.linearVelocity = Vector3.zero;
        //    // forward 방향으로 물리적인 힘 가하기
        //    rb.linearVelocity = rot * Vector3.forward * (playerData.ATTSPEED > 0 ? playerData.ATTSPEED : 10f);
        //}
    }
    public void ApplyKickback()
    {
        if (rb != null)
        {
            // 현재 바라보고 있는 방향의 반대 방향 계산
            Vector3 backDirection = -transform.forward;

            // 순간적인 충격량(Impulse)을 가함
            rb.AddForce(backDirection * kickbackForce, ForceMode.Impulse);
        }
    }
}