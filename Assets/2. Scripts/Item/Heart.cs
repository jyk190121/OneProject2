using UnityEngine;

public class Heart : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            //플레이어 체력회복 100
            BaseUnit player = other.GetComponent<BaseUnit>();
            player.Heal(100f);
            Destroy(gameObject);
        }
    }
}
