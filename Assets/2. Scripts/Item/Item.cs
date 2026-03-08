using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "ScriptableObject/Item")]
public class Item : ScriptableObject
{
    //HP회복, 공격력증가(총알), 스피드증가(총알, 플레이어)
    [Header("스텟강화")]
    public float heal;
    public float att;
    public float attSpeed;
    public float moveSpeed;

    [Header("아이템 오브젝트")]
    public GameObject itemPrefab;

    [Header("아이템 발동효과")]
    public GameObject effect;
}
