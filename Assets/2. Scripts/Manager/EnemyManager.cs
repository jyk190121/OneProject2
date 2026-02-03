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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SpawnEnemy(enemies));
    }

    IEnumerator SpawnEnemy(List<Enemy> enemies)
    {
        int count = 0;

        yield return new WaitForSeconds(3f);

        for (int i =0;  i < 30; i++)
        {
            int r = Random.Range(0, 12);
            if (enemies.Count <= count)
            {
                count = 0;
            }

            GameObject enemyObj = Instantiate(enemies[count].PREFAB, enemyPos[r]);

            count++;

            yield return new WaitForSeconds(1f);
            //enemyList.Remove(enemyList[i]);
        }
    }
}
