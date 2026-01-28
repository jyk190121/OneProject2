using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "ScriptableObject/Item")]
public class Item : ScriptableObject
{
    [Header("아이템 효과")]
    public GameObject EFFECT;
}
