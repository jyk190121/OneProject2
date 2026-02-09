using UnityEngine;
using UnityEngine.AI;
public class MoveController
{
    Rigidbody rb;
    float rotationSpeed = 5f;     // 회전 속도 (높을수록 빠르게 회전)
    float avoidRange = 1.5f;      // 장애물 감지 거리
    //NavMeshAgent agent; // Rigidbody 대신 사용

    // MoveController 내부 필드
    Vector3 smoothedDirection;

    int combinedLayerMask;

    public GameObject currentBlockingWall; // 현재 앞을 막고 있는 벽 저장

    public MoveController(Rigidbody rb)
    {
        this.rb = rb;
        // 레이어 마스크를 미리 계산하여 캐싱
        combinedLayerMask = (1 << LayerMask.NameToLayer("Wall")) | (1 << LayerMask.NameToLayer("D_Wall"));
    }

    public void Move(Vector3 direction, float speed)
    {
        // 벽 정보 초기화
        currentBlockingWall = null;

        Vector3 avoidanceDir = CalculateAvoidanceDirection(direction);

        if (avoidanceDir == Vector3.zero)
        {
            // 이동은 안 하지만 몸은 플레이어를 향해 부드럽게 회전
            Quaternion lookRot = Quaternion.LookRotation(direction.normalized);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRot, rotationSpeed * Time.deltaTime));
            return;
        }

        if (smoothedDirection == Vector3.zero) smoothedDirection = avoidanceDir;
        smoothedDirection = Vector3.Slerp(smoothedDirection, avoidanceDir, 0.15f);

        rb.linearVelocity = new Vector3(smoothedDirection.x * speed, rb.linearVelocity.y, smoothedDirection.z * speed);

        // 회전 처리
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
        float radius = 0.4f;

        Vector3 normalizedTarget = targetDir.normalized;

        // SphereCast로 정면의 넓은 범위 감지
        if (Physics.SphereCast(origin, radius, normalizedTarget, out hit, 2.0f, combinedLayerMask))
        {
            // 만약 감지된 물체가 D_Wall 레이어라면 변수에 저장
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("D_Wall"))
            {
                currentBlockingWall = hit.collider.gameObject;
            }

            Vector3 hitNormal = hit.normal;
            hitNormal.y = 0;
            Vector3 slideDir = Vector3.ProjectOnPlane(normalizedTarget, hitNormal).normalized;

            bool leftBlocked = Physics.Raycast(origin, Quaternion.Euler(0, -45, 0) * normalizedTarget, 5.0f, combinedLayerMask);
            bool rightBlocked = Physics.Raycast(origin, Quaternion.Euler(0, 45, 0) * normalizedTarget, 5.0f, combinedLayerMask);

            if (leftBlocked && rightBlocked) return Vector3.zero;

            return (slideDir + hitNormal * 0.2f).normalized;
        }

        //만약 정면은 괜찮은데 대각선이 걸릴 수도 있으니 스캔 추가
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

    public void Stop()
    {
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
    }
}
