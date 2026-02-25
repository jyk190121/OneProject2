using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiPlayerSessionManager : NetworkBehaviour
{
    public static MultiPlayerSessionManager Instance { get; private set; }

    ISession Activesssion { get; set; }
    public ISession ActiveSession => Activesssion;

    public event Action<bool> onHostChanged;
    public event Action<string, string> updateSessionInfo;

    const string LOBBY_SCENE_NAME = "Lobby";
    const string GAME_SCENE_NAME = "Multi";
    const string START_SCENE_NAME = "StartScene";

    public GameObject playerPrefab;
    public GameObject scoreManagerPrefab;

    private bool isLeaving = false; // 퇴장 중인지 확인하는 플래그

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        try
        {
            //멀티플레이어 초기화
            await UnityServices.InitializeAsync();
            //익명로그인
            //각 플레이어를 고유한 식별을 불러옴
            //if (!AuthenticationService.Instance.IsSignedIn)
            //{
            //    await AuthenticationService.Instance.SignInAnonymouslyAsync();
            //    print($"익명 로그인 성공 {AuthenticationService.Instance.PlayerId}");
            //}
            if (NetworkManager.Singleton != null)
            {
                // 중복 방지를 위해 한 번 빼고 더하기
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }
        catch (Exception e)
        {
            print($"초기화 실패 {e.Message}");
        }
    }

    //// 1. OnNetworkSpawn에서 연결 끊김 콜백을 등록합니다.
    //public override void OnNetworkSpawn()
    //{
    //    if (NetworkManager.Singleton != null)
    //    {
    //        // 클라이언트가 서버(호스트)와의 연결이 끊겼을 때 실행될 함수 등록
    //        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    //    }
    //}

    //public override void OnNetworkDespawn()
    //{
    //    if (NetworkManager.Singleton != null)
    //    {
    //        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    //    }
    //}

    // 2. 호스트가 나갔을 때 실행될 로직
    private void OnClientDisconnected(ulong clientId)
    {
        //if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        //{
        //    if (clientId == NetworkManager.ServerClientId || clientId == NetworkManager.Singleton.LocalClientId)
        //    {
        //        Debug.Log("<color=red>서버와의 연결이 종료되었습니다</color>");
        //        LeaveSession();
        //    }
        //}
        // 클라이언트 입장에서 서버(호스트)와의 연결이 끊겼을 때
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            // 서버 ID가 끊겼거나, 자신의 ID가 끊겼을 때 (강퇴 혹은 방 터짐)
            if (clientId == NetworkManager.ServerClientId || clientId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log("<color=red>호스트가 게임을 종료했거나 연결이 끊겼습니다. 메인으로 이동합니다.</color>");

                // 여기서 즉시 씬 이동 로직 실행
                StopAllCoroutines();
                LeaveSession();
            }
        }

    }

    // 세션 생성/참가 로직에서 이벤트를 구독하도록 수정
    private void SubscribeToSessionEvents()
    {
        if (Activesssion == null) return;

        // 세션 정보(인원, 소유자 등)가 변경될 때마다 호출됨
        Activesssion.Changed += () =>
        {
            if (isLeaving) return; // 나가는 중이면 무시
            Debug.Log($"세션 정보 변경 감지됨. 소유자 여부: {Activesssion.IsHost}");

            CheckHostMigration();

            // 인원 변경 시 UI 즉시 갱신 시도
            RefreshGameStartUI();
            //Debug.Log("세션 정보 변경 감지됨");

            //// 1. 호스트 변경 체크 (기존 로직)
            //CheckHostMigration();

            //// 2. 인원 변경에 따른 UI 갱신 (추가)
            //// 클라이언트가 나갔을 때 호스트의 UI에서 [게임 시작] 버튼을 숨기거나 갱신해야 함
            //var gameStartUI = FindFirstObjectByType<GameStart>();
            //if (gameStartUI != null)
            //{
            //    gameStartUI.SetupUI();
            //}
        };
    }

    private void CheckHostMigration()
    {
        // 퇴장 중이라면 호스트 승계 로직을 타지 않음
        if (Activesssion == null || isLeaving) return;

        if (Activesssion.IsHost && !NetworkManager.Singleton.IsServer)
        {
            Debug.Log("방장이 나가서 호스트 권한이 넘어왔습니다. 방을 폭파합니다.");
            LeaveSession();
            return;
        }

        // 그 외 일반적인 UI 갱신
        onHostChanged?.Invoke(Activesssion.IsHost);
        RefreshGameStartUI();
    }
    //IEnumerator MigrateToHostRoutine()
    //{
    //    //// 1. 기존 클라이언트 연결 종료
    //    //NetworkManager.Singleton.Shutdown();

    //    //// 2. 종료될 때까지 대기
    //    //yield return new WaitUntil(() => !NetworkManager.Singleton.IsListening);
    //    //yield return new WaitForSeconds(0.5f);

    //    //// 3. 스스로 호스트(서버)로 시작
    //    //NetworkManager.Singleton.StartHost();

    //    //// 4. 서버 시작 완료 대기 후 UI 갱신
    //    //yield return new WaitUntil(() => NetworkManager.Singleton.IsServer);

    //    //var gameStartUI = FindFirstObjectByType<GameStart>();
    //    //if (gameStartUI != null) gameStartUI.SetupUI();

    //    //print("호스트 전환 및 UI 갱신 완료");

    //    // 1. 기존 클라이언트 연결 종료
    //    NetworkManager.Singleton.Shutdown();
    //    yield return new WaitUntil(() => !NetworkManager.Singleton.IsListening);
    //    yield return new WaitForSeconds(0.5f);

    //    // 2. 중요: 새 호스트로서 새로운 Relay Allocation 생성
    //    var relayTask = RelayService.Instance.CreateAllocationAsync(2);
    //    while (!relayTask.IsCompleted) yield return null;
    //    if (relayTask.IsFaulted) { Debug.LogError("릴레이 생성 실패"); yield break; }

    //    var allocation = relayTask.Result;
    //    var joinCodeTask = RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
    //    while (!joinCodeTask.IsCompleted) yield return null;
    //    string newJoinCode = joinCodeTask.Result;

    //    // 3. 새로운 릴레이 데이터를 Transport에 설정
    //    var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
    //    transport.SetHostRelayData(
    //        allocation.RelayServer.IpV4,
    //        (ushort)allocation.RelayServer.Port,
    //        allocation.AllocationIdBytes,
    //        allocation.Key,
    //        allocation.ConnectionData
    //    );

    //    // 4. 세션 정보 업데이트 (새로운 JoinCode를 세션에 등록해야 다른 사람이 보고 들어옴)
    //    // Multiplayer Service에서 지원하는 경우 세션의 코드를 갱신하는 로직이 필요합니다.
    //    // (보통 자동 연동되지만, 안될 경우 세션 속성에 명시적으로 기록하기도 합니다)

    //    // 5. 호스트 시작
    //    NetworkManager.Singleton.StartHost();
    //    yield return new WaitUntil(() => NetworkManager.Singleton.IsServer);

    //    // 중요: 호스트가 되었으므로 이벤트를 한 번 더 호출하여 버튼을 활성화
    //    onHostChanged?.Invoke(true);

    //    RefreshGameStartUI();
    //}
    void RefreshGameStartUI()
    {
        var gameStartUI = FindFirstObjectByType<GameStart>();
        if (gameStartUI != null)
        {
            gameStartUI.SetupUI();
        }
    }
    //호스트 방 생성
    public async void CreateSessionAsync(string sessionName)
    {
        isLeaving = false; // 혹시 남아있을 플래그 초기화

        try
        {
            // 재시작 시 안전장치: 기존 상태가 남아있다면 강제 종료
            if (NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();

            // 중요: 서비스 재로그인을 통해 깨끗한 상태 확보
            await EnsureSignedInAsync();

            // 이 사이에 지연을 살짝 주어 서비스 상태가 완전히 갱신되게 합니다.
            await System.Threading.Tasks.Task.Delay(500);

            //세션 옵션설정
            var options = new SessionOptions
            {
                //세션 이름 (로비 목록)
                Name = sessionName,
                IsPrivate = false,
                IsLocked = false,       //true인 경우 게임 시작 후 새로운 플레이어 참가불가
                MaxPlayers = 2
            }.WithRelayNetwork();
            //릴레이 서버 자동할당
            //플레이어(클라이언트)들이 IP주소 몰라도 연결가능

            Activesssion = await MultiplayerService.Instance.CreateSessionAsync(options);

            // 릴레이 할당 정보 추출 (Activession.Code가 릴레이 조인 코드와 연결)
            var allocation = await RelayService.Instance.CreateAllocationAsync(2);
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            SubscribeToSessionEvents();

            GameSceneManager.Instance.LoadScene(LOBBY_SCENE_NAME);

            NetworkManager.Singleton.StartHost();
            await System.Threading.Tasks.Task.Delay(500);

            print($"세션 생성 완료 / 이름 : {Activesssion.Name} , 코드 {Activesssion.Code}");
            print("릴레이 서버 연결 완료");

            updateSessionInfo?.Invoke(Activesssion.Name, Activesssion.Code);
        }
        catch (Exception e)
        {
            print($"세션 생성 실패 {e.Message}");
        }
    }

    //클라이언트 참가
    public async void QuickJoinSessionAsync()
    {
        try
        {
            isLeaving = false; // 플래그 초기화

            // 1. 초기화 강도 높이기
            //if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            // 중요: 이전 세션 객체가 남아있다면 명시적으로 null 처리
            Activesssion = null;

            await EnsureSignedInAsync();
            await System.Threading.Tasks.Task.Delay(500);

            //// 1. 기존 연결 및 데이터 완전 초기화
            //var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            //if (NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();

            // 중요: 이전 릴레이 데이터가 남아있지 않도록 초기화
            //transport.SetRelayServerData(new Unity.Networking.Transport.Relay.RelayServerData());
            //await EnsureSignedInAsync();
            //await System.Threading.Tasks.Task.Delay(500);

            var joinOptions = new QuickJoinOptions();
            var matchmakingOptions = new SessionOptions { MaxPlayers = 2, IsLocked = false, IsPrivate = false };

            // 퀵 조인 실행
            Activesssion = await MultiplayerService.Instance.MatchmakeSessionAsync(joinOptions, matchmakingOptions);

            if (Activesssion != null)
            {
                // 2. 현재 방장(플레이어2)이 만든 최신 Relay JoinCode를 가져옴
                string relayJoinCode = Activesssion.Code;
                Debug.Log($"세션 접속 성공. 릴레이 코드: {relayJoinCode}");

                //var joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
                var joinAllocation = await RelayService.Instance.JoinAllocationAsync(Activesssion.Code);

                // 3. 새로운 호스트(플레이어2)의 릴레이 정보로 세팅
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetClientRelayData(
                    joinAllocation.RelayServer.IpV4,
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.Key,
                    joinAllocation.ConnectionData,
                    joinAllocation.HostConnectionData
                );

                SubscribeToSessionEvents();

                // 씬 이동을 먼저 해서 NetworkManager가 활동할 바닥을 깔아줌
                GameSceneManager.Instance.LoadScene(LOBBY_SCENE_NAME);

                // 릴레이 서버가 바뀐 정보를 인지할 수 있게 약간의 여유를 줌
                await System.Threading.Tasks.Task.Delay(1000);

                // 4. 클라이언트 시작
                bool success = NetworkManager.Singleton.StartClient();
                Debug.Log($"Netcode 클라이언트 시작 시도 결과: {success}");

                if (success)
                {
                    Debug.Log("<color=cyan>Netcode 클라이언트 연결 시도 중...</color>");
                    onHostChanged?.Invoke(false);
                    updateSessionInfo?.Invoke(Activesssion.Name, Activesssion.Code);
                    StartCoroutine(DelayedSetupUI());
                }
                else
                {
                    Debug.LogError("Netcode 클라이언트 시작 실패");
                    LeaveSession(); // 실패 시 안전하게 퇴장 처리
                }

                // 조인 시점에 클라이언트임을 명시적으로 알림
                //onHostChanged?.Invoke(false);

                //updateSessionInfo?.Invoke(Activesssion.Name, Activesssion.Code);
                //StartCoroutine(DelayedSetupUI());
            }
        }
        catch (Exception e)
        {
            print($"퀵 조인 실패 {e.Message}");
        }
    }

    IEnumerator DelayedSetupUI()
    {
        yield return null;
        var ui = FindFirstObjectByType<GameStart>();
        if (ui != null) ui.SetupUI();
    }

    //public void StartSession()
    //{
    //    NetworkManager.Singleton.SceneManager.LoadScene(GAME_SCENE_NAME, UnityEngine.SceneManagement.LoadSceneMode.Single);
    //}

    void StartSession()
    {
        //if (NetworkManager.Singleton.IsServer)LeaveSession
        //{
        //    // 중요: 씬 로드 이벤트를 구독합니다. (함수와 이벤트를 연결)
        //    NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnMultiSceneLoaded;

        //    var status = NetworkManager.Singleton.SceneManager.LoadScene(
        //        GAME_SCENE_NAME,
        //        UnityEngine.SceneManagement.LoadSceneMode.Single
        //    );

        //    if (status != SceneEventProgressStatus.Started)
        //    {
        //        Debug.LogWarning("씬 로드 시작 실패: " + status);
        //        // 실패 시 이벤트 해제
        //        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnMultiSceneLoaded;
        //    }
        //}

        if (NetworkManager.Singleton.IsServer)
        {
            // 실제 Netcode상에 연결된 클라이언트가 나(호스트) 포함 2명인지 확인
            // 만약 릴레이 연결 지연으로 인해 아직 1명이라면 조금 더 기다려야 합니다.
            if (NetworkManager.Singleton.ConnectedClientsList.Count < 2)
            {
                Debug.LogWarning("아직 모든 클라이언트가 Netcode 서버에 접속되지 않았습니다. 잠시 후 다시 시도합니다.");
                // 리트라이 로직을 넣거나, 대기 시간을 더 줍니다.
                return;
            }

            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnMultiSceneLoaded;

            var status = NetworkManager.Singleton.SceneManager.LoadScene(
                GAME_SCENE_NAME,
                UnityEngine.SceneManagement.LoadSceneMode.Single
            );

            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogWarning("씬 로드 시작 실패: " + status);
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnMultiSceneLoaded;
            }
        }
    }

    public async void LeaveSession()
    {
        if (isLeaving) return;
        isLeaving = true; // 퇴장 시작 알림

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("Netcode 서버/클라이언트 셧다운 완료");
        }

        if (Activesssion != null)
        {
            try
            {
                // 이벤트 구독 해제 (이게 가장 중요합니다. 자신이 나갈 때 이벤트가 호출되는 것 방지)
                //Activesssion.Changed -= () => { };
                Activesssion.Changed -= CheckHostMigration;

                // 클라이언트는 세션 퇴장을 기다려주는 것이 재접속에 유리합니다.
                await Activesssion.LeaveAsync();
                Debug.Log("세션 서비스 퇴장 완료");
            }
            catch (Exception e) { Debug.LogWarning($"세션 퇴장 중 오류: {e.Message}"); }
            finally { Activesssion = null; }
        }

        //if (NetworkManager.Singleton != null)
        //{
        //    NetworkManager.Singleton.Shutdown();
        //}

        if (AuthenticationService.Instance.IsSignedIn)
        {
            // SignOut을 하면 PlayerId가 바뀌어 새로운 유저로 인식되므로 재참여가 원활해집니다.
            AuthenticationService.Instance.SignOut();
        }

        //Activesssion = null;

        await System.Threading.Tasks.Task.Delay(500);
        isLeaving = false; // 이동 전 플래그 초기화
        SceneManager.LoadScene(START_SCENE_NAME);
    }

    public async System.Threading.Tasks.Task EnsureSignedInAsync()
    {
        try
        {
            // 초기화가 안 되어 있다면 실행
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            // 이미 로그인되어 있다면 로그아웃 (확실하게 새 ID를 받기 위함)
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }

            // 새로운 익명 로그인 시도
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            print($"새로운 익명 로그인 성공: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            print($"로그인 재시도 실패: {e.Message}");
        }
    }

    public IEnumerator WaitForPlayersRoutine()
    {
        //while (Activesssion != null)
        //{
        //    // 2명이 찼는지 확인
        //    if (Activesssion.Players.Count >= 2)
        //    {
        //        print("2인 접속 확인, 3초 후 게임 시작!");
        //        yield return new WaitForSeconds(3f);
        //        StartSession();
        //        yield break; // 코루틴 종료
        //    }
        //    else
        //    {
        //        print("1인 플레이는 싱글플레이를 이용해주세요");
        //    }

        //    yield return new WaitForSeconds(1f); // 1초마다 확인
        //}

        float timeout = 20f; // 최대 20초 대기
        float timer = 0f;

        while (Activesssion != null)
        {
            // 1. 세션 인원 확인
            if (Activesssion.Players.Count >= 2)
            {
                // 2. Netcode 실제 연결 확인
                if (NetworkManager.Singleton.ConnectedClientsList.Count >= 2)
                {
                    print("모든 네트워크 연결 확인됨. 게임을 시작합니다!");
                    yield return new WaitForSeconds(1f);
                    StartSession();
                    yield break;
                }
                else
                {
                    timer += 0.5f;
                    print($"플레이어 접속 대기 중... ({NetworkManager.Singleton.ConnectedClientsList.Count}/2) - {timer}s");
                }
            }
            else
            {
                print("세션에 다른 플레이어가 들어오기를 기다리는 중...");
            }

            // 타임아웃 처리
            if (timer >= timeout)
            {
                print("연결 타임아웃: 클라이언트가 세션에는 들어왔으나 네트워크 연결에 실패했습니다.");
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
    void OnMultiSceneLoaded(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName == GAME_SCENE_NAME && IsServer)
        {
            // 이벤트가 중복 실행되지 않도록 한 번 실행 후 바로 해제
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnMultiSceneLoaded;

            SpawnManagers();

            foreach (ulong clientId in clientsCompleted)
            {
                SpawnPlayerForClient(clientId);
            }
        }
    }

    // 소환 로직 별도 분리 (가독성 및 유지보수)
    private void SpawnPlayerForClient(ulong clientId)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("PlayerPrefab이 할당되지 않았습니다!");
            return;
        }

        // 클라이언트 ID에 따라 위치 분기 (0번 호스트, 1번 클라이언트)
        Vector3 spawnPos = (clientId == 0) ? new Vector3(-3, 1, 0) : new Vector3(3, 1, 0);

        GameObject playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();

        // 이 함수가 실제로 네트워크 상에 객체를 생성하고 소유권을 부여함
        netObj.SpawnAsPlayerObject(clientId);

        print($"플레이어 소환 완료: ClientId {clientId}");
    }

    // 세션 인원수 확인용 헬퍼 함수 (LobbyManager 등에서 사용)
    public int GetPlayerCount()
    {
        return Activesssion?.Players?.Count ?? 0;
    }

    void SpawnManagers()
    {
        // ScoreManager 생성 및 스폰
        if (scoreManagerPrefab != null)
        {
            GameObject scoreObj = Instantiate(scoreManagerPrefab);
            scoreObj.GetComponent<NetworkObject>().Spawn();
            print("ScoreManager 네트워크 스폰 완료");
        }
    }
}
