using UnityEngine;

public class JoystickPlayer : MonoBehaviour
{
    public float speed;
    public VariableJoystick variableJoystick;
    public Rigidbody rb;

    // 회전 제어 변수 (기본값 true)
    public bool canRotate = true;
    public void FixedUpdate()
    {
        Vector3 direction = Vector3.forward * variableJoystick.Vertical + Vector3.right * variableJoystick.Horizontal;

        //rb.AddForce(direction * speed * Time.fixedDeltaTime, ForceMode.Impulse);
        rb.linearVelocity = direction * speed;


        //rb.rotation = Quaternion.LookRotation(direction * speed * Time.deltaTime);
        // 입력값의 크기가 아주 작을 때(손을 뗐을 때)는 회전하지 않도록 함
        if (canRotate && direction.sqrMagnitude > 0.01f)
        {
            // 현재 방향을 바라보는 회전값 생성
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // 즉시 회전시키거나, Lerp를 사용하여 부드럽게 회전시킬 수 있음
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 10f);
        }
    }
}