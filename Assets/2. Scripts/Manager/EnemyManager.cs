using UnityEngine;
using System.Collections.Generic;
using System.Collections;
/// <summary>
/// Enemy Spawn 
/// 위치 설정 Clamp (맵 Size)
/// 생성 속도 (2 ~ 5초)
/// Enemy Type 랜덤(A ~ F)
/// 최대 생성 Enemy 수? 
/// </summary>
public class EnemyManager : MonoBehaviour
{
    public List<Enemy> enemies;                     // 모든 적 타입 리스트
    public List<Transform> enemyPos;                // 적 생성 위치
    List<Enemy> enemyList = new List<Enemy>();      // 에너미 돌리기


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemyList = enemies;

        StartCoroutine(SpawnEnemy(enemies));
    }

    IEnumerator SpawnEnemy(List<Enemy> enemies)
    {
        yield return new WaitForSeconds(3f);

        for (int i =0;  i < 10; i++)
        {
            GameObject enemyObj = Instantiate(enemyList[i].PREFAB, enemyPos[i]);

            yield return new WaitForSeconds(1f);
            enemyList.Remove(enemyList[i]);

            if (enemyList == null)
            {
                enemyList = enemies;
            }
        }
    }
}
