using UnityEngine;
/// <summary>
/// 생성 후 자동파괴
/// </summary>
public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f; // 삭제될 시간 (3초)

    void Start()
    {
        // 생성되자마자 lifeTime 후에 이 게임 오브젝트를 삭제하도록 예약
        Destroy(gameObject, lifeTime);
    }

    // (선택 사항) 무언가에 부딪혔을 때 즉시 삭제하고 싶다면
    private void OnCollisionEnter(Collision collision)
    {

        if(collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            // 벽이나 적에 부딪히면 즉시 삭제
            Destroy(gameObject);
        }
    }
}
