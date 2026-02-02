using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "ScriptableObject/Enemy")]

public class Enemy : ScriptableObject
{
    [Header("적 타입")]
    public string type;

    [Header("적 속성")]
    public float HP;
    public float ATT;
    [Header("스피드는 낮을수록 빠르게")]
    [Range(0f, 1f)]
    public float MOVESPEED;
    [Range(0f, 1f)]
    public float ATTSPEED;

    [Header("보유 총")]
    public GameObject GUN;

    //게임 진행 중 얻고 사용할 아이템
    [Header("보유 아이템")]
    public List<Item> ITEMS;

    [Header("사망 시 표시될 이팩트")]
    public GameObject DIEEFFECT;
}
