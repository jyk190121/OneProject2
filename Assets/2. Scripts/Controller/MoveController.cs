using UnityEngine;
using UnityEngine.AI;
public class MoveController
{
    Rigidbody rb;
    float rotationSpeed = 10f;   // 회전 속도 (높을수록 빠르게 회전)
    float avoidRange = 1.5f;      // 장애물 감지 거리
    //NavMeshAgent agent; // Rigidbody 대신 사용

    // MoveController 내부 필드
    Vector3 smoothedDirection;

    int combinedLayerMask;

    public MoveController(Rigidbody rb)
    {
        this.rb = rb;
        // 레이어 마스크를 미리 계산하여 캐싱
        combinedLayerMask = (1 << LayerMask.NameToLayer("Wall")) | (1 << LayerMask.NameToLayer("D_Wall"));
    }

    public void Move(Vector3 direction, float speed)
    {
        // 1. 회피 방향 계산
        Vector3 avoidanceDir = CalculateAvoidanceDirection(direction);

        // 2. 부드러운 방향 전환 (0.1f -> 0.3f로 약간 높여 반응성 강화)
        if (smoothedDirection == Vector3.zero) smoothedDirection = avoidanceDir;
        smoothedDirection = Vector3.Slerp(smoothedDirection, avoidanceDir, 0.3f);

        // 3. 이동 처리: Y축 속도는 유지하여 중력 영향 받게 함
        rb.linearVelocity = new Vector3(smoothedDirection.x * speed, rb.linearVelocity.y, smoothedDirection.z * speed);

        // 4. 회전 처리
        if (smoothedDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(smoothedDirection);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.deltaTime));
        }

        // 디버깅용 레이 (씬 뷰에서 확인 가능)
        Debug.DrawRay(rb.position + Vector3.up * 0.5f, smoothedDirection * avoidRange, Color.green);
    }
    private Vector3 CalculateAvoidanceDirection(Vector3 targetDir)
    {
        Vector3 origin = rb.position + Vector3.up * 0.5f;
        RaycastHit hit;
        int combinedLayerMask = (1 << LayerMask.NameToLayer("Wall")) | (1 << LayerMask.NameToLayer("D_Wall"));
        float radius = 0.4f;

        // 1. 아주 가까운 거리(예: 0.7f)에서 벽 충돌 감지
        // avoidRange가 너무 길면 오히려 길찾기가 꼬입니다. 몸체보다 약간 큰 정도가 좋습니다.
        if (Physics.SphereCast(origin, radius , targetDir, out hit, 1.0f, combinedLayerMask))
        {
            // [핵심] 벽의 수직 벡터(Normal)를 가져옵니다.
            Vector3 hitNormal = hit.normal;
            hitNormal.y = 0; // 수평 이동만 고려

            // [핵심] 가려던 방향(targetDir)을 벽면(hitNormal)에 투영하여 미끄러지는 방향 계산
            Vector3 slideDir = Vector3.ProjectOnPlane(targetDir, hitNormal).normalized;

            // 약간의 회피 힘을 더해 벽에서 살짝 떨어지게 유도
            return (slideDir + hitNormal * 0.2f).normalized;
        }

        // 2. 만약 정면은 괜찮은데 대각선이 걸릴 수도 있으니 스캔 추가
        for (float angle = 30f; angle <= 90f; angle += 30f)
        {
            if (Physics.Raycast(origin, Quaternion.Euler(0, angle, 0) * targetDir, out hit, 0.8f, combinedLayerMask))
            {
                return (Quaternion.Euler(0, -angle, 0) * targetDir).normalized;
            }
            if (Physics.Raycast(origin, Quaternion.Euler(0, -angle, 0) * targetDir, out hit, 0.8f, combinedLayerMask))
            {
                return (Quaternion.Euler(0, angle, 0) * targetDir).normalized;
            }
        }

        return targetDir;
    }


    //public void Move(Vector3 targetPosition, float speed)
    //{
    //    agent.speed = speed;
    //    agent.SetDestination(targetPosition); // 경로를 알아서 계산함 (벽 회피 포함)
    //}

    public void Stop()
    {
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
    }
}
