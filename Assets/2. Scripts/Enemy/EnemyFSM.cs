using System.Data;
using UnityEngine;
/// <summary>
/// 이동/공격/음직임
/// </summary>
public class EnemyFSM : MonoBehaviour
{
    enum State
    {
        Idle,
        Move,
        Die
    }

    //상태 (기본값 Idle)
    State currentState = State.Idle;

    //사망여부 체크
    bool isDead = false;

    //최적화 시키기 위한 변수 (거리 계산)
    float chaseRangeSqr;
    float chaseRange = 10f;

    Rigidbody rb;
    Transform targetPlayer;

    MoveController moveController;
    BattleManager battleManager;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        moveController = new MoveController(rb);
        battleManager = FindAnyObjectByType<BattleManager>();

        if (targetPlayer == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) targetPlayer = player.transform;
        }

        chaseRangeSqr = chaseRange * chaseRange;

    }

    // Update is called once per frame
    void Update()
    {
        if(isDead || targetPlayer == null) return;

        UpdateState();
    }

    void UpdateState()
    {
        //float distanceSqr = (targetPlayer.position - transform.position).sqrMagnitude;

        switch (currentState)
        {
            case State.Idle:
                HandleIdleState();
                break;
            case State.Move:
                HandleMoveState();
                break;
            //case State.Attack:
            //    HandleAttackState();
            //    break;
                //case State.Hit: Hit();
                //break;
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

    void HandleMoveState()
    {
        if (battleManager.isStarting)
        {
            currentState = State.Idle;
            return;
        }
        Vector3 direction = (targetPlayer.position - transform.position);
        direction.y = 0; // 높이 차이로 인해 땅을 보거나 하늘을 보지 않도록 고정

        if (direction.magnitude > 0.1f)
        {
            moveController.Move(direction.normalized, 1.5f);
        }
        //Vector3 direction = (targetPlayer.position - transform.position).normalized;
        //moveController.Move(direction, 2f);
    }

}
