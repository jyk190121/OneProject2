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

    [SerializeField] private int maxEnemyCount = 300; // 최대 적 수
    [SerializeField] private float spawnDelay = 0.5f; // 생성 간격

    private List<GameObject> enemyList = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (enemies == null || enemies.Count == 0) return;
        StartCoroutine(SpawnEnemyRoutine());
    }

    //IEnumerator SpawnEnemy(List<Enemy> enemies)
    //{
    //    //int count = Random.Range(0, 10);
    //    //int enemyCount = Random.Range(30, 101);

    //    //yield return new WaitForSeconds(3f);

    //    //for (int i =0;  i < enemyCount; i++)
    //    //{
    //    //    int r = Random.Range(0, 12);
    //    //    if (enemies.Count <= count)
    //    //    {
    //    //        count = 0;
    //    //    }

    //    //    foreach (Transform pos in enemyPos)
    //    //    {
    //    //        GameObject enemyObj = Instantiate(enemies[count].PREFAB, enemyPos.position, Quaternion.identity);
    //    //    }

    //    //    count++;

    //    //    yield return new WaitForSeconds(1f);
    //    //    //enemyList.Remove(enemyList[i]);
    //    //}
    //    yield return new WaitForSeconds(3f);

    //    int currentSpawnCount = 0;
    //    //int targetCount = Random.Range(100, 1001); // 100~1000마리 생성 시도
    //    //List<GameObject> enemyList = new List<GameObject>();

    //    while (enemyList.Count <= 100)
    //    {
    //        ////플레이어 사망 시 모든 Enemy 삭제 
    //        //if (createEnemyStop)
    //        //{
    //        //    print("여길 타나");
    //        //    enemyList.Clear();
    //        //    break;
    //        //}

    //        // BattleManager의 상태나 자신의 플래그 확인
    //        if (createEnemyStop || BattleManager.Instance.isGameOver)
    //        {
    //            // 이미 생성된 적들을 지우고 싶다면 아래 로직 실행
    //            foreach (var enemy in enemyList)
    //            {
    //                if (enemy != null) Destroy(enemy);
    //            }
    //            enemyList.Clear();
    //            yield break; // 코루틴 완전히 종료
    //        }

    //        // 1. 적 타입 랜덤 선택
    //        int enemyIndex = Random.Range(0, enemies.Count);

    //        // 2. 생성 지점 랜덤 선택 (enemyPos의 자식을 루프 돌지 않고 배열에서 선택)
    //        if (spawnPoints.Length > 0)
    //        {
    //            int posIndex = Random.Range(0, spawnPoints.Length);
    //            Transform targetPos = spawnPoints[posIndex];

    //            // 생성
    //            GameObject enemyObj = Instantiate(enemies[enemyIndex].PREFAB, targetPos.position, Quaternion.identity);
    //            yield return new WaitForSeconds(0.5f); // 0.5초 간격으로 한 마리씩 생성
    //            enemyList.Add(enemyObj);
    //        }
    //        else
    //        {
    //            // 지정된 스폰 포인트가 없으면 매니저 위치에서 생성
    //            GameObject enemyObj = Instantiate(enemies[enemyIndex].PREFAB, transform.position, Quaternion.identity);
    //            yield return new WaitForSeconds(0.5f); // 0.5초 간격으로 한 마리씩 생성
    //            enemyList.Add(enemyObj);
    //        }
    //        currentSpawnCount++;

    //        if(currentSpawnCount >= 99)
    //        {
    //            yield return new WaitForSeconds(10f);
    //            currentSpawnCount = 0;
    //        }
    //    }
    //}

    IEnumerator SpawnEnemyRoutine()
    {
        yield return new WaitForSeconds(3f);

        while (true)
        {
            print($"현재 몇마리 :{enemyList.Count}");
            // 게임 오버 시 정리 로직 (루프 나오자)
            if (BattleManager.Instance.isGameOver || createEnemyStop)
            {
                CleanupEnemies();
                yield break;
            }

            // 죽어서 Destroy된 적들을 목록에서 빼주어야 정확한 카운트가 가능합니다.
            enemyList.RemoveAll(item => item == null);

            if (enemyList.Count < maxEnemyCount)
            {
                SpawnRandomEnemy();
                // 생성 간격 대기
                yield return new WaitForSeconds(spawnDelay);
            }
            else
            {
                //다 찼다면 잠시 대기 후 다시 체크 (CPU 부하 감소)
                yield return new WaitForSeconds(1f);
            }
        }
    }

    private void SpawnRandomEnemy()
    {
        int enemyIndex = Random.Range(0, enemies.Count);
        Transform spawnPos = GetRandomSpawnPoint();

        GameObject enemyObj = Instantiate(enemies[enemyIndex].PREFAB, spawnPos.position, Quaternion.identity);
        enemyList.Add(enemyObj);
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints.Length > 0)
        {
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }
        return transform;
    }

    private void CleanupEnemies()
    {
        foreach (var enemy in enemyList)
        {
            if (enemy != null) Destroy(enemy);
        }
        enemyList.Clear();
    }
}
