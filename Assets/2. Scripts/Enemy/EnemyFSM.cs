using System.Collections;
using Unity.Netcode;
using UnityEngine;
/// <summary>
/// 이동/공격/음직임
/// </summary>
public class EnemyFSM : BaseUnit
{
    [Header("데이터 설정")]
    public Enemy enemyData; // 스크립터블 오브젝트 할당

    enum State
    {
        Idle,
        Move,
        Attack,
        Die
    }

    //상태 (기본값 Idle)
    State currentState = State.Idle;

    //사망여부 체크
    //bool isDead = false;

    //최적화 시키기 위한 변수 (거리 계산)
    float attackSqr;
    float attackRange = 5f;

    Rigidbody rb;
    Animator anim;
    Transform targetPlayer;

    MoveController moveController;
    AnimController animController;
    BattleManager battleManager;

    public Transform bulletPos;

    bool isAttacking = false;

    void Awake()
    {
        if (enemyData != null)
        {
            // SO에서 스탯 가져와서 현재 체력 초기화
            InitStats(enemyData.HP);

            SetDeathEffect(enemyData.DIEEFFECT);

            // 속도 등 다른 스탯 적용
            // 예: moveController.SetSpeed(enemyData.SPEED);
        }

        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        moveController = new MoveController(rb);
        animController = new AnimController(anim);
        battleManager = FindAnyObjectByType<BattleManager>();

        if (targetPlayer == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) targetPlayer = player.transform;
        }

        attackSqr = attackRange * attackRange;

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //if (!IsServer) return;

        // 싱글 모드이거나 멀티 서버인 경우에만 AI 작동
        bool canUpdateAI = (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) || IsServer;
        if (!canUpdateAI) return;

        targetPlayer = GetNearestPlayer();

        if (isDead || targetPlayer == null) return;

        UpdateState();
    }

    void UpdateState()
    {
        //if (!IsServer) return;
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isNetworkActive && !IsServer) return;

        // [추가] 공격 애니메이션이 시작되었거나 공격 딜레이 중이라면 이동 로직을 타지 않음
        if (isAttacking)
        {
            moveController.Stop();
            return;
        }

        Vector3 direction = (targetPlayer.position - transform.position);
        // 높이 차이로 인해 땅을 보거나 하늘을 보지 않도록 고정
        direction.y = 0;
        bool isPathBlockedByDWall = (currentState == State.Move && moveController.currentBlockingWall != null);

        if (direction.sqrMagnitude <= attackSqr || isPathBlockedByDWall)
        {
            currentState = State.Attack;
        }
        else
        {
            currentState = State.Move;
        }

        switch (currentState)
        {
            case State.Idle: HandleIdleState(); break;
            case State.Move: HandleMoveState(direction); break;
            case State.Attack:
                // 만약 벽 때문에 공격하는 거라면 벽을 향해 방향 수정
                Vector3 attackDir = isPathBlockedByDWall ?
                    (moveController.currentBlockingWall.transform.position - transform.position).normalized :
                    direction;
                HandleAttackState(attackDir);
                break;
        }
    }

    void HandleIdleState()
    {
        if(!battleManager.isStarting)
        {
            currentState = State.Move;
            return;
        }
        moveController.Stop();
    }

    void HandleMoveState(Vector3 direction)
    {
        if (battleManager.isStarting)
        {
            currentState = State.Idle;
            return;
        }
        float moveSpeed = enemyData.MOVESPEED;

        moveController.Move(direction.normalized, moveSpeed);

        //Vector3 direction = (targetPlayer.position - transform.position).normalized;
        //moveController.Move(direction, 2f);
    }


    void HandleAttackState(Vector3 direction)
    {
        moveController.Stop();
        
        //총알발사
        if (!isAttacking)
        {
            StartCoroutine(Attack(direction));
        }
  
    }

    IEnumerator Attack(Vector3 direction)
    {
        isAttacking = true;

        animController.PlayAttack();
        float timer = 0f;
        float rotaionSpeed = 10f;
        float attSpeed = enemyData.ATTSPEED; 

        while (timer < attSpeed)
        {
            timer += Time.deltaTime;
            // 적 본체를 플레이어 방향으로 회전 (Y축만 회전하여 위아래로 기우는 것 방지)
            Vector3 lookDir = direction;
            lookDir.y = 0; // 적이 바닥을 보거나 하늘을 보지 않도록 고정
            if (lookDir != Vector3.zero)
            {
                moveController.Stop();
                //transform.rotation = Quaternion.LookRotation(lookDir);
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotaionSpeed);
            }

            yield return null;

        }

        // 멀티플레이: 서버가 Rpc로 전파
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (IsServer)
            {
                FireEnemyBulletClientRpc(bulletPos.position, transform.rotation, enemyData.ATT);
            }
        }
        // 싱글플레이: 즉시 로컬 발사 함수 호출
        else
        {
            ExecuteEnemyLocalFire(bulletPos.position, transform.rotation, enemyData.ATT);
        }

        yield return new WaitForSeconds(1.5f);
        isAttacking = false;
    }

    [ClientRpc]
    private void FireEnemyBulletClientRpc(Vector3 pos, Quaternion rot, float damage)
    {
        // 실제 총알 생성 및 물리 세팅 (각 클라이언트의 로컬 풀에서 수행)
        ExecuteEnemyLocalFire(pos, rot, damage);
    }

    private void ExecuteEnemyLocalFire(Vector3 pos, Quaternion rot, float damage)
    {
        if (enemyData.GUN == null) return;

        // 1. 로컬 풀에서 총알 꺼내기
        GameObject bulletObj = BulletEnemyPoolManager.Instance.GetBullet(enemyData.GUN);
        bulletObj.transform.position = pos;

        // 2. 방향 및 회전 설정
        Vector3 fireDir = rot * Vector3.forward;
        if (enemyData.type == "B")
        {
            bulletObj.transform.position = pos + (Vector3.down * 0.5f);
            bulletObj.transform.rotation = Quaternion.LookRotation(fireDir) * Quaternion.Euler(90f, 0, 0f);
        }
        else
        {
            bulletObj.transform.rotation = Quaternion.LookRotation(fireDir);
        }

        // 3. 데미지 및 물리 속도 설정
        EnemyBullet enemyBulletScript = bulletObj.GetComponent<EnemyBullet>();
        if (enemyBulletScript != null)
        {
            enemyBulletScript.SetDamage(damage);

            Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                // 적의 총알 속도 (필요시 SO에 추가하거나 고정값 사용)
                rb.linearVelocity = fireDir * 10f;
            }
        }
    }

    protected override void Die()
    {
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        if (isNetworkActive)
        {
            // 서버일 때만 점수를 올립니다. 클라이언트일 때는 무시합니다.
            // 서버가 totalScore.Value를 바꾸면, NetworkVariable의 특성상 
            // 모든 클라이언트의 UI는 자동으로 업데이트됩니다.
            if (IsServer)
            {
                //print("여길 타려나(서버 스코어)");
                ScoreManager.Instance.AddScoreServer(enemyData.SCORE);
            }
        }
        // 싱글플레이 모드인 경우
        else
        {
            ScoreManager.Instance.AddScoreServer(enemyData.SCORE);
        }

        base.Die(); // 공통 로직(애니메이션, 이펙트 등) 실행
    }

    Transform GetNearestPlayer()
    {
        // BattleManager에 등록된 플레이어 리스트 활용
        var players = BattleManager.Instance.joystickPlayers;
        Transform nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (var p in players)
        {
            if (p == null) continue;

            float distance = Vector3.Distance(transform.position, p.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = p.transform;
            }
        }
        return nearest;
    }
}
