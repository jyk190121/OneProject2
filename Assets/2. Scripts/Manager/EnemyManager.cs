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
    public Transform[] spawnPoints;                 // 적 생성 위치
    public bool createEnemyStop = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (enemies == null || enemies.Count == 0) return;
        StartCoroutine(SpawnEnemy(enemies));
    }

    IEnumerator SpawnEnemy(List<Enemy> enemies)
    {
        //int count = Random.Range(0, 10);
        //int enemyCount = Random.Range(30, 101);

        //yield return new WaitForSeconds(3f);

        //for (int i =0;  i < enemyCount; i++)
        //{
        //    int r = Random.Range(0, 12);
        //    if (enemies.Count <= count)
        //    {
        //        count = 0;
        //    }

        //    foreach (Transform pos in enemyPos)
        //    {
        //        GameObject enemyObj = Instantiate(enemies[count].PREFAB, enemyPos.position, Quaternion.identity);
        //    }

        //    count++;

        //    yield return new WaitForSeconds(1f);
        //    //enemyList.Remove(enemyList[i]);
        //}
        yield return new WaitForSeconds(3f);

        int currentSpawnCount = 0;
        int targetCount = Random.Range(100, 1001); // 100~1000마리 생성 시도
        List<GameObject> enemyList = new List<GameObject>();

        while (currentSpawnCount < targetCount)
        {
            ////플레이어 사망 시 모든 Enemy 삭제 
            //if (createEnemyStop)
            //{
            //    print("여길 타나");
            //    enemyList.Clear();
            //    break;
            //}

            // BattleManager의 상태나 자신의 플래그 확인
            if (createEnemyStop || BattleManager.Instance.isGameOver)
            {
                // 이미 생성된 적들을 지우고 싶다면 아래 로직 실행
                foreach (var enemy in enemyList)
                {
                    if (enemy != null) Destroy(enemy);
                }
                enemyList.Clear();
                yield break; // 코루틴 완전히 종료
            }

            // 1. 적 타입 랜덤 선택
            int enemyIndex = Random.Range(0, enemies.Count);

            // 2. 생성 지점 랜덤 선택 (enemyPos의 자식을 루프 돌지 않고 배열에서 선택)
            if (spawnPoints.Length > 0)
            {
                int posIndex = Random.Range(0, spawnPoints.Length);
                Transform targetPos = spawnPoints[posIndex];

                // 생성
                GameObject enemyObj = Instantiate(enemies[enemyIndex].PREFAB, targetPos.position, Quaternion.identity);
                yield return new WaitForSeconds(0.5f); // 0.5초 간격으로 한 마리씩 생성
                enemyList.Add(enemyObj);
            }
            else
            {
                // 지정된 스폰 포인트가 없으면 매니저 위치에서 생성
                GameObject enemyObj = Instantiate(enemies[enemyIndex].PREFAB, transform.position, Quaternion.identity);
                yield return new WaitForSeconds(0.5f); // 0.5초 간격으로 한 마리씩 생성
                enemyList.Add(enemyObj);
            }

            currentSpawnCount++;

            if(targetCount == currentSpawnCount)
            {
                yield return new WaitForSeconds(3f);
                currentSpawnCount = 0;
            }
        }
    }
}
