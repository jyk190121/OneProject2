using UnityEngine;
/// <summary>
/// 생성 후 자동파괴
/// </summary>
public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f;        // 삭제될 시간 (3초)

    public GameObject bulletEffect;                      // 총알이 다른 프리팹에 닿았을 떄(벽, 적 등) 이팩트
    public GameObject wallEffect;                        // 부서지는 벽 이팩트


    void Start()
    {
        // 생성되자마자 lifeTime 후에 이 게임 오브젝트를 삭제하도록 예약
        Destroy(gameObject, lifeTime);
    }

    // (선택 사항) 무언가에 부딪혔을 때 즉시 삭제하고 싶다면
    private void OnCollisionEnter(Collision collision)
    {
        int targetLayer = collision.gameObject.layer;
        bool isWall = targetLayer == LayerMask.NameToLayer("Wall");
        bool isDWall = targetLayer == LayerMask.NameToLayer("D_Wall");

        if (isWall || isDWall)
        {
            ContactPoint contact = collision.contacts[0];
            Quaternion rot = Quaternion.LookRotation(contact.normal);
            Vector3 pos = contact.point;

            if (bulletEffect != null)
            {
                GameObject bEffectInstance = Instantiate(bulletEffect, pos, rot);
                Destroy(bEffectInstance, 0.5f);
            }

            if (isDWall)
            {
                if (wallEffect != null)
                {
                    // 중요: 생성된 인스턴스를 변수에 할당해야 함
                    GameObject effectInstance = Instantiate(wallEffect, pos, rot);
                    Destroy(effectInstance, 0.5f);
                }

                Destroy(collision.gameObject);
            }

            // 총알 자신 삭제
            Destroy(gameObject);
        }
    }
}
