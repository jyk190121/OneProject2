using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
//using Unity.Cinemachine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;
    public List<JoystickPlayer> joystickPlayers = new List<JoystickPlayer>();
    public bool isStarting;
    public Image countImg;
    public TextMeshProUGUI countTxt;
    //List<Enemy> enemies;
    //Player player;
    public VariableJoystick joystick;
    public Image playerHP_bar;

    public GameObject box_A;       // 떨어지는 블록 (밀림)
    public GameObject box_B;       // 떨어지는 블록 (밀림)
    public GameObject wallB;       // Y축 고정 블록 (안밀림)

    // [추가] 생성된 블록들을 추적하기 위한 리스트
    private List<GameObject> activeBlocks = new List<GameObject>();

    // [추가] 맵 범위 설정
    private float limitX = 50f;
    private float limitZ = 50f;
    private float limitY = -10f; // 낭떠러지 체크용

    GameObject[] block = new GameObject[3];

    public bool isGameOver = false;

    public BulletSpawner bulletSpawner; // 인스펙터에서 할당하거나 Find로 찾기
    private int lastAssignedIndex = -1;
    bool isLoopStarted = false;

    private void OnEnable()
    {

        isStarting = true;

        StartCoroutine(StartDelayRoutine());
        //StartCoroutine(BlockSpawnLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(Instance);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        if (bulletSpawner == null)
        {
            bulletSpawner = FindFirstObjectByType<BulletSpawner>();
        }

        block[0] = box_A;
        block[1] = box_B;
        block[2] = wallB;
        Application.targetFrameRate = 60;
    }

    void Update()
    {
        // 게임 중일 때만 체크 (성능을 위해 FixedUpdate에서 호출해도 좋습니다)
        if (!isGameOver)
        {
            CheckBlocksBounds();
            //if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            //{
            //    if (!isLoopStarted) // 변수 하나 추가: bool isLoopStarted = false;
            //    {
            //        isLoopStarted = true;
            //        StartCoroutine(BlockSpawnLoop());
            //    }
            //}

            // [수정] 멀티플레이 서버이거나, 아예 네트워크 매니저가 없는(싱글) 상태일 때 루프 시작
            bool isSingleMode = NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;
            bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

            if (isSingleMode || isServer)
            {
                if (!isLoopStarted)
                {
                    isLoopStarted = true;
                    StartCoroutine(BlockSpawnLoop());
                }
            }
        }
    }

    public void RegisterPlayer(JoystickPlayer player)
    {
        if (!joystickPlayers.Contains(player))
        {
            joystickPlayers.Add(player);

            // 현재 이 플레이어가 로컬 플레이어라면 UI 연결
            if (player.IsOwner)
            {

                RefreshUIReferences(); // 씬 이동 후라면 여기서 UI를 새로 잡음

                player.variableJoystick = this.joystick;
                player.HP_BAR = this.playerHP_bar;

                // BulletSpawner에도 내 캐릭터를 가장 먼저 등록
                if (bulletSpawner != null) bulletSpawner.SetTargetPlayer(player);
            }


            if (joystickPlayers.Count == 1)
            {
                AssignNextPlayerToSpawner();
            }
        }
    }

    // 순차적으로 다음 플레이어를 스포너에게 알려주는 함수
    public void AssignNextPlayerToSpawner()
    {
        if (joystickPlayers.Count == 0) return;

        // 다음 인덱스 계산 (리스트 범위를 벗어나면 0으로 돌아감)
        lastAssignedIndex = (lastAssignedIndex + 1) % joystickPlayers.Count;

        JoystickPlayer nextPlayer = joystickPlayers[lastAssignedIndex];
        bulletSpawner.SetTargetPlayer(nextPlayer);
    }

    // 씬이 바뀌었을 때 Null이 된 참조들을 다시 찾아주는 함수
    private void RefreshUIReferences()
    {
        // 만약 기존 참조가 사라졌다면(Missing) 다시 찾기
        if (joystick == null) joystick = FindFirstObjectByType<VariableJoystick>();
        if (bulletSpawner == null) bulletSpawner = FindFirstObjectByType<BulletSpawner>();

        // HP바 같은 경우 캔버스 아래 특정 이름으로 찾는 것이 좋습니다.
        if (playerHP_bar == null)
        {
            GameObject hpObj = GameObject.Find("PlayerHPbar"); // 씬 내 오브젝트 이름
            if (hpObj != null) playerHP_bar = hpObj.GetComponent<Image>();
        }
    }

    IEnumerator StartDelayRoutine()
    {
        //yield return new WaitForSecondsRealtime(0.2f); // 최소한의 숨 고르기
        //yield return new WaitUntil(() => Time.deltaTime < 0.05f); // 프레임 드랍이 멈출 때까지 대기

        //countImg.gameObject.SetActive(true);

        //countTxt.text = "3";
        //yield return new WaitForSecondsRealtime(1f);

        //countTxt.text = "2";
        //yield return new WaitForSecondsRealtime(1f);

        //countTxt.text = "1";
        //yield return new WaitForSecondsRealtime(1f);

        yield return new WaitForSecondsRealtime(0.3f);

        if (!isStarting) yield break;

        string[] counts = { "3", "2", "1" };
        foreach (var c in counts)
        {
            countTxt.text = c;
            yield return new WaitForSecondsRealtime(1f);
        }

        //countTxt.text = "3";
        //yield return new WaitForSeconds(0.554f);
        ////yield return new WaitForSeconds(1f);
        //countTxt.text = "2";
        //yield return new WaitForSeconds(1f);
        //countTxt.text = "1";
        //yield return new WaitForSeconds(1f);

        countTxt.color = Color.blue;
        countTxt.text = "GO!!";
        isStarting = false;

        yield return new WaitForSeconds(0.5f);
        // countLabel.style.display = DisplayStyle.None; // UI 숨기기
        countImg.gameObject.SetActive(false);
        countTxt.text = "";
    }
    //IEnumerator StartBlockCreate()
    //{
    //    int r = Random.Range(0, 2);
    //    int ea = Random.Range(0, 10);

    //    yield return new WaitForSeconds(3f);

    //    StartCoroutine(CreateBlcok(r, ea));

    //    yield return new WaitForSeconds(15f);
    //}

    //IEnumerator CreateBlcok(int r, int ea)
    //{
    //    for (int i = 0; i < ea; i++)
    //    {
    //        float clampX = Random.Range(-6f, 6.1f);
    //        float clampZ = Random.Range(-45f, 45.1f);
    //        float y = -0.5f;

    //        if (r == 0) y = Mathf.Clamp(y, 0f, 15f);

    //        yield return new WaitForSeconds(3f);

    //        transPos.position = new Vector3(clampX, y, clampZ);

    //        yield return new WaitForSeconds(3f);
            
    //        Instantiate(block[r], transPos);
    //    }
    //}

    // [추가] 블록 생성을 주기적으로 반복하는 루프
    IEnumerator BlockSpawnLoop()
    {
        while (!isGameOver) // 게임이 끝날 때까지 반복
        {
            yield return new WaitForSeconds(5f);

            if (isGameOver) break; // 대기 시간 중에 게임이 끝났을 경우 탈출

            int r = Random.Range(0, 3);

            int ea = Random.Range(1, 10);

            yield return StartCoroutine(CreateBlock(r, ea));
        }
    }

    IEnumerator CreateBlock(int r, int ea)
    {
        //if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) yield break;

        // [1] 멀티플레이 중인데 서버가 아니라면 중단
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isNetworkActive && !NetworkManager.Singleton.IsServer) yield break;

        for (int i = 0; i < ea; i++)
        {
            float clampX = Random.Range(-45f, 45.1f);
            float clampZ = Random.Range(-45f, 45.1f);
            float y = (r != 2) ? Random.Range(0f, 15f) : -0.5f;

            Vector3 spawnPosition = new Vector3(clampX, y, clampZ);

            GameObject newBlock = Instantiate(block[r], spawnPosition, Quaternion.identity);

            //if (newBlock.TryGetComponent<NetworkObject>(out var netObj))
            //{
            //    netObj.Spawn();
            //    newBlock.transform.position = spawnPosition;
            //}

            // [2] 멀티플레이 시에는 Spawn() 호출, 싱글 시에는 그냥 생성 유지
            if (isNetworkActive && newBlock.TryGetComponent<NetworkObject>(out var netObj))
            {
                netObj.Spawn();
                newBlock.transform.position = spawnPosition;
            }

            activeBlocks.Add(newBlock);

            yield return new WaitForSeconds(1f); // 블록 하나 생성 후 대기 시간
        }
    }

    public void GameOver()
    {
        // 이미 게임 오버라면 중복 실행 방지
        if (isGameOver) return;

        // 1. 현재 살아있는 플레이어 수 체크
        int alivePlayers = 0;
        foreach (var p in joystickPlayers)
        {
            // activeSelf뿐만 아니라 체력이나 별도의 isDead 플래그가 있다면 함께 체크하는 것이 좋습니다.
            if (p != null && p.gameObject.activeSelf && !p.GetDeadStatus())
            {
                alivePlayers++;
            }
        }

        // 2. 생존자가 남아있다면 게임 오버를 시키지 않고 리턴
        if (alivePlayers > 0)
        {
            UpdateSpawnerToAlivePlayer();
            return;
        }

        // 3. 모든 플레이어가 죽었을 때만 실행되는 로직
        isGameOver = true;

        // 에너미 매니저에게 정지 신호 전달
        EnemyManager em = FindFirstObjectByType<EnemyManager>();
        if (em != null) em.createEnemyStop = true;

        // 필요하다면 여기서 결과창 UI를 띄우는 RPC 호출
    }
    public void UpdateSpawnerToAlivePlayer()
    {
        JoystickPlayer survivor = joystickPlayers.Find(p => p != null &&
                                                        p.gameObject.activeInHierarchy &&
                                                        !p.GetDeadStatus());
        //// 리스트에서 죽지 않은(isDead == false) 첫 번째 플레이어를 찾습니다.
        //JoystickPlayer survivor = joystickPlayers.Find(p => p != null && !p.GetDeadStatus());

        if (survivor != null && bulletSpawner != null)
        {
            bulletSpawner.SetTargetPlayer(survivor);
            print($"[Target Change] 새 타겟: {survivor.name}");
        }
        // 시네머신 카메라 타겟 변경
        var vcam = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam == null || GameSceneManager.Instance.SceneName() == "Single") return;
        if (vcam != null)
        {
            vcam.Target.TrackingTarget = survivor.transform;
            vcam.Target.LookAtTarget = survivor.transform;
            print($"[Camera] 관전 타겟이 {survivor.name}로 변경되었습니다.");
        }

    }

    private void CheckBlocksBounds()
    {
        //if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        // [1] 멀티플레이 중인데 서버가 아니라면 중단
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isNetworkActive && !NetworkManager.Singleton.IsServer) return;


        // [중요] 리스트 순회 시 원소를 삭제해야 하므로 반드시 역순(for)으로 순회합니다.
        for (int i = activeBlocks.Count - 1; i >= 0; i--)
        {
            GameObject target = activeBlocks[i];

            // 이미 파괴되었거나 null인 경우 리스트에서 제거
            if (target == null)
            {
                activeBlocks.RemoveAt(i);
                continue;
            }

            Vector3 pos = target.transform.position;

            // 설정한 범위를 하나라도 벗어났는지 체크
            if (Mathf.Abs(pos.x) > limitX || Mathf.Abs(pos.z) > limitZ || pos.y < limitY)
            {
                //if (target.TryGetComponent<NetworkObject>(out var netObj))
                //{
                //    netObj.Despawn();
                //}

                // [2] 네트워크 객체이고 네트워크가 활성 상태면 Despawn, 아니면 일반 Destroy
                if (isNetworkActive && target.TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
                {
                    netObj.Despawn();
                }
                else
                {
                    Destroy(target);
                }

                // 리스트에서 제거
                activeBlocks.RemoveAt(i);

                print($"박스가 범위를 벗어나 제거되었습니다: {pos}");
            }
        }
    }
}
