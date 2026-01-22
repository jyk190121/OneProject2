using UnityEngine;
using System.Collections;
public class BulletPooling : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f;

    // 오브젝트가 활성화될 때마다 실행됨
    void OnEnable()
    {
        // 코루틴을 사용하여 일정 시간 후 비활성화
        StartCoroutine(DeactivateAfterTime());
    }

    IEnumerator DeactivateAfterTime()
    {
        yield return new WaitForSeconds(lifeTime);

        // 삭제 대신 비활성화 (풀로 돌려보냄)
        gameObject.SetActive(false);
    }

    // 코루틴 중복 실행을 방지하기 위해 비활성화될 때 멈춰줌
    void OnDisable()
    {
        StopAllCoroutines();
    }
}