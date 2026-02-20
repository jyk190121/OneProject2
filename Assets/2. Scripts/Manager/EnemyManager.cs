using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
/// <summary>
/// Enemy Spawn 
/// 위치 설정 Clamp (맵 Size)
/// 생성 속도 (2 ~ 5초)
/// Enemy Type 랜덤(A ~ F)
/// 최대 생성 Enemy 수? 
/// </summary>
public class EnemyManager : NetworkBehaviour
{
    public List<Enemy> enemies;                     // 모든 적 타입 리스트
    public Transform[] spawnPoints;                 // 적 생성 위치
    public bool createEnemyStop = false;

    [SerializeField] private int maxEnemyCount = 300; // 최대 적 수
    [SerializeField] private float spawnDelay = 0.5f; // 생성 간격

    private List<GameObject> enemyList = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{
    //    if (enemies == null || enemies.Count == 0) return;
    //    StartCoroutine(SpawnEnemyRoutine());
    //}

    void Start()
    {
        //싱글모드
        if(NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            StartCoroutine(SpawnEnemyRoutine());
        }
    }

    public override void OnNetworkSpawn()
    {
        // 중요: 서버(Host)만 적을 생성하는 코루틴을 시작합니다.
        if (IsServer)
        {
            StartCoroutine(SpawnEnemyRoutine());
        }
    }

    IEnumerator SpawnEnemyRoutine()
    {
        yield return new WaitForSeconds(3f);

        while (true)
        {
            //print($"현재 몇마리 :{enemyList.Count}");
            // 게임 오버 시 정리 로직 (루프 나오자)
            if (BattleManager.Instance.isGameOver || createEnemyStop)
            {
                //CleanupEnemies();
                RequestCleanup(); // 서버에서 삭제 명령
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
        
        ////네트워크 동기화
        //if (enemyObj.TryGetComponent<NetworkObject>(out var netObj))
        //{
        //    netObj.Spawn();
        //}

        // 멀티 - 네트워크 활성화 상태이고 서버일 때만 Spawn() 호출
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (IsServer && enemyObj.TryGetComponent<NetworkObject>(out var netObj))
            {
                netObj.Spawn();
            }
        }

        enemyList.Add(enemyObj);
    }

    // 코루틴이나 Update 등에서 적을 정리해야 할 때 호출하는 방식
    private void RequestCleanup()
    {
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        if (isNetworkActive)
        {
            // 멀티플레이 중이라면 서버에 요청 (서버라면 즉시 실행됨)
            CleanupEnemiesServerRpc();
        }
        else
        {
            // 싱글플레이 모드라면 직접 실행
            InternalCleanup();
        }
    }

    [ServerRpc]
    private void CleanupEnemiesServerRpc()
    {
        // 서버 환경에서 리스트의 적들을 네트워크 상에서 제거
        InternalCleanup();

        ////if (!IsServer) return;
        //bool canUpdateAI = (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) || IsServer;
        //if (!canUpdateAI) return;

        //foreach (var enemy in enemyList)
        //{
        //    if (enemy != null)
        //    {
        //        // Destroy 대신 NetworkObject의 Despawn 혹은 Destroy를 호출
        //        var netObj = enemy.GetComponent<NetworkObject>();
        //        if (netObj != null && netObj.IsSpawned) netObj.Despawn();
        //        else Destroy(enemy);
        //    }
        //}
        //enemyList.Clear();
    }

    // 싱글/멀티 공통으로 적을 파괴하는 핵심 로직
    private void InternalCleanup()
    {
        foreach (var enemy in enemyList)
        {
            if (enemy != null)
            {
                var netObj = enemy.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                    netObj.Despawn();
                else
                    Destroy(enemy);
            }
        }
        enemyList.Clear();
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints.Length > 0)
        {
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }
        return transform;
    }

    //private void CleanupEnemies()
    //{
    //    foreach (var enemy in enemyList)
    //    {
    //        if (enemy != null) Destroy(enemy);
    //    }
    //    enemyList.Clear();
    //}
}
