using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 멀티플레이를 생각하여 SO로 만들자
/// </summary>

[CreateAssetMenu(fileName = "NewPlayer", menuName = "ScriptableObject/Player")]
public class Player : ScriptableObject
{
    [Header("플레이어 속성")]
    public float HP;
    public float SPEED;
    public float ATT;

    [Header("보유 총")]
    public GameObject GUN;

    //게임 진행 중 얻고 사용할 아이템
    [Header("보유 아이템")]
    public List<Item> ITEMS; 

    [Header("사망 시 표시될 이팩트")]
    public GameObject DIEEFFECT;
}
