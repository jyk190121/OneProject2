using UnityEngine;
using Key = UnityEngine.InputSystem.Key;

public class JoystickPlayer : BaseUnit
{
    //public float speed;
    public VariableJoystick variableJoystick;
    public Rigidbody rb;

    // 회전 제어 변수 (기본값 true)
    public bool canRotate = true;

    public Player playerData; // P1_Data, P2_Data 등을 각각 할당

    void Start()
    {
        BattleManager.Instance.RegisterPlayer(this);

        if (playerData != null)
        {
            InitStats(playerData.HP);

            SetDeathEffect(playerData.DIEEFFECT);
            // playerData.SPEED 등을 사용하여 이동 속도 설정
        }
    }

    public void FixedUpdate()
    {
        // 3초 대기 중이라면 아래 로직을 모두 건너뜀
        if (BattleManager.Instance.isStarting)
        {
            // 혹시 모를 밀림 방지
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // 조이스틱 값 + 키보드(Input.cs) 값을 더합니다.
        float h = variableJoystick.Horizontal + Input.GetAxis("Horizontal");
        float v = variableJoystick.Vertical + Input.GetAxis("Vertical");

        // 방향 벡터 생성 (3D 환경이므로 x와 z축 사용)
        Vector3 direction = (Vector3.forward * v) + (Vector3.right * h);

        // 대각선 이동 속도 보정 (길이가 1을 넘지 않도록 normalized)
        if (direction.magnitude > 1f)
        {
            direction.Normalize();
        }

        //Vector3 direction = Vector3.forward * variableJoystick.Vertical + Vector3.right * variableJoystick.Horizontal;

        //rb.AddForce(direction * speed * Time.fixedDeltaTime, ForceMode.Impulse);
        rb.linearVelocity = direction * playerData.MOVESPEED;

        //rb.rotation = Quaternion.LookRotation(direction * speed * Time.deltaTime);
        // 입력값의 크기가 아주 작을 때(손을 뗐을 때)는 회전하지 않도록 함
        if (canRotate && direction.sqrMagnitude > 0.01f)
        {
            // 현재 방향을 바라보는 회전값 생성
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // 즉시 회전시키거나, Lerp를 사용하여 부드럽게 회전시킬 수 있음
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 5f);
        }
    }

    protected override void Die()
    {
        base.Die(); // 공통 로직(이펙트 생성 등) 실행

        // 플레이어 전용: 매니저에게 게임 오버 알림
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.GameOver();
        }

        // 애니메이션 실행 후 삭제 등 추가 로직
         Destroy(gameObject);
    }
}