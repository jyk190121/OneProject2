using System.Collections;
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
    void Update()
    {
        //if(currentHP <= 0 )
        //{
        //    Die();
        //}

        if(isDead || targetPlayer == null) return;

        UpdateState();
    }

    void UpdateState()
    {
        //float distanceSqr = (targetPlayer.position - transform.position).sqrMagnitude;
        Vector3 direction = (targetPlayer.position - transform.position);
        direction.y = 0; // 높이 차이로 인해 땅을 보거나 하늘을 보지 않도록 고정

        switch (currentState)
        {
            case State.Idle:
                HandleIdleState();
                break;
            case State.Move:
                HandleMoveState(direction);
                break;
            case State.Attack:
                HandleAttackState(direction);
                break;
        }

        if (direction.sqrMagnitude > attackSqr)
        {
            currentState = State.Move;
        }
        else
        {
            currentState = State.Attack;
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
        float moveSpeed = (1.2f - enemyData.MOVESPEED) * 5f;

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
        float rotaionSpeed = 20f;
        float attSpeed = (1.2f - enemyData.ATTSPEED); 

        while (timer < attSpeed)
        {
            timer += Time.deltaTime;
            // 적 본체를 플레이어 방향으로 회전 (Y축만 회전하여 위아래로 기우는 것 방지)
            Vector3 lookDir = direction;
            lookDir.y = 0; // 적이 바닥을 보거나 하늘을 보지 않도록 고정
            if (lookDir != Vector3.zero)
            {
                //transform.rotation = Quaternion.LookRotation(lookDir);
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotaionSpeed);
            }

            yield return null;
        }

        //yield return new WaitForSeconds(0.5f);

        GameObject bulletObj = BulletEnemyPoolManager.Instance.GetBullet();

        //bulletObj.transform.SetParent(null);
        bulletObj.transform.position = bulletPos.position;
        //bulletObj.transform.rotation = Quaternion.LookRotation(direction);
        bulletObj.transform.rotation = transform.rotation;

        EnemyBullet enemyBulletScript = bulletObj.GetComponent<EnemyBullet>();

        enemyBulletScript.SetDamage(enemyData.ATT);

        if (enemyBulletScript != null)
        {
            Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                //rb.linearVelocity = Vector3.zero; // 이전 속도 초기화
                //rb.linearVelocity = direction * 10f; // 새 방향으로 발사
                rb.linearVelocity = transform.forward * 10f;
            }
        }

        // 공격 쿨타임 (다음 공격까지 대기)
        yield return new WaitForSeconds(1.5f);

        isAttacking = false;
    }
}
