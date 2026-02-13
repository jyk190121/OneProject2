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

    //총알?
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
        //if(currentHP <= 0 )
        //{
        //    Die();
        //}
        if (!IsServer) return;

        targetPlayer = GetNearestPlayer();

        if (isDead || targetPlayer == null) return;

        UpdateState();
    }

    void UpdateState()
    {
        if (!IsServer) return;

        //float distanceSqr = (targetPlayer.position - transform.position).sqrMagnitude;
        Vector3 direction = (targetPlayer.position - transform.position);
        direction.y = 0; // 높이 차이로 인해 땅을 보거나 하늘을 보지 않도록 고정
        bool isPathBlockedByDWall = (currentState == State.Move && moveController.currentBlockingWall != null);

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

        //switch (currentState)
        //{
        //    case State.Idle:
        //        HandleIdleState();
        //        break;
        //    case State.Move:
        //        HandleMoveState(direction);
        //        break;
        //    case State.Attack:
        //        HandleAttackState(direction);
        //        break;
        //}

        // --- 벽 파괴 로직 추가 ---
        // 이동 중인데 앞에 D_Wall이 감지되었다면 공격 상태로 전환

        if (direction.sqrMagnitude <= attackSqr || isPathBlockedByDWall)
        {
            currentState = State.Attack;
        }
        else
        {
            currentState = State.Move;
        }

        //if (direction.sqrMagnitude > attackSqr)
        //{
        //    currentState = State.Move;
        //}
        //else
        //{
        //    currentState = State.Attack;
        //}

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

        // [중요] 서버에서만 발사 정보를 모든 클라이언트에게 전파
        if (IsServer)
        {
            // 서버에서 계산된 현재 총구 위치와 회전값을 보냅니다.
            FireEnemyBulletClientRpc(bulletPos.position, transform.rotation, enemyData.ATT);
        }

        yield return new WaitForSeconds(1.5f); // 공격 후 딜레이
        isAttacking = false;

        //yield return new WaitForSeconds(0.5f);
        //GameObject bulletObj = BulletEnemyPoolManager.Instance.GetBullet(enemyData.GUN);

        //GameObject bulletObj = BulletEnemyPoolManager.Instance.GetBullet();

        ////bulletObj.transform.SetParent(null);
        //bulletObj.transform.position = bulletPos.position;
        ////bulletObj.transform.rotation = Quaternion.LookRotation(direction);
        //bulletObj.transform.rotation = transform.rotation;

        //EnemyBullet enemyBulletScript = bulletObj.GetComponent<EnemyBullet>();

        //enemyBulletScript.SetDamage(enemyData.ATT);

        //if (enemyBulletScript != null)
        //{
        //    Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
        //    if (rb != null)
        //    {
        //        //rb.linearVelocity = Vector3.zero; // 이전 속도 초기화
        //        //rb.linearVelocity = direction * 10f; // 새 방향으로 발사
        //        rb.linearVelocity = transform.forward * 5f;
        //    }
        //}

        //// [수정] SO에 등록된 총알 프리팹을 인자로 전달하여 가져옴
        //if (enemyData.GUN != null)
        //{
        //    // GetBullet에 적마다 다른 총알 프리팹을 전달
        //    GameObject bulletObj = BulletEnemyPoolManager.Instance.GetBullet(enemyData.GUN); // GUN 필드 사용
        //    bulletObj.transform.position = bulletPos.position;
        //    Vector3 fireDir = transform.forward;
        //    //Vector3 fireDirLow = fireDir + Vector3.down;

        //    if (enemyData.type == "B")
        //    {
        //        //bulletObj.transform.rotation = transform.rotation * Quaternion.Euler(90f, 0, 0f);
        //        bulletObj.transform.position = bulletPos.position + (Vector3.down * 0.5f);
        //        bulletObj.transform.rotation = Quaternion.LookRotation(fireDir) * Quaternion.Euler(90f, 0, 0f);
        //    }
        //    else
        //    {
        //        bulletObj.transform.rotation = Quaternion.LookRotation(fireDir);
        //    }

        //    EnemyBullet enemyBulletScript = bulletObj.GetComponent<EnemyBullet>();
        //    if (enemyBulletScript != null)
        //    {
        //        enemyBulletScript.SetDamage(enemyData.ATT);

        //        Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
        //        if (rb != null)
        //        {
        //            //Vector3 targetPos = targetPlayer.position + Vector3.up * 1.0f;
        //            //rb.linearVelocity = transform.forward * 5f; // 속도는 SO에 맞춰 조절 가능

        //            // 물리 속도 초기화 필수 (풀링된 오브젝트이므로)
        //            rb.linearVelocity = Vector3.zero;
        //            rb.angularVelocity = Vector3.zero;

        //            // 설정한 정면 방향으로 발사
        //            rb.linearVelocity = fireDir * 10f;
        //        }
        //    }
        //}

        //// 공격 쿨타임 (다음 공격까지 대기)
        //yield return new WaitForSeconds(1.5f);

        //isAttacking = false;
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
        //// 에너미 전용: 점수 획득이나 특정 아이템 드랍 로직 추가 가능
        //ScoreManager.Instance.ScoreUpdateUI(enemyData.SCORE);
        if (IsServer)
        {
            // 서버에서 점수를 계산하고, 모든 유저에게 UI 업데이트를 지시합니다.
            // ScoreManager가 NetworkBehaviour라면 내부에서 ClientRpc를 호출하는 것이 좋습니다.
            ScoreManager.Instance.AddScoreServer(enemyData.SCORE);
        }
        base.Die(); // 공통 로직 실행
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
