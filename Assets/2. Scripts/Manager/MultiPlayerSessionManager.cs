using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode.Transports.UTP;

public class MultiPlayerSessionManager : NetworkBehaviour
{
    public static MultiPlayerSessionManager Instance { get; private set; }

    ISession Activesssion { get; set; }

    public ISession ActiveSession => Activesssion;

    // 세션 소유자가 변경되었을 때 실행될 이벤트 (UI 갱신용)
    public event Action<bool> onHostChanged;

    const string LOBBY_SCENE_NAME = "Lobby";
    const string GAME_SCENE_NAME = "Multi";

    public event Action<string, string> updateSessionInfo;

    public GameObject playerPrefab;
    public GameObject scoreManagerPrefab;

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
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                print($"익명 로그인 성공 {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (Exception e)
        {
            print($"초기화 실패 {e.Message}");
        }
    }

    // 세션 생성/참가 로직에서 이벤트를 구독하도록 수정
    private void SubscribeToSessionEvents()
    {
        if (Activesssion == null) return;

        // 세션 정보(인원, 소유자 등)가 변경될 때마다 호출됨
        Activesssion.Changed += () =>
        {
            Debug.Log("세션 정보 변경 감지됨");
            CheckHostMigration();
        };
    }

    private void CheckHostMigration()
    {
        if (Activesssion == null) return;

        // 현재 내 PlayerId가 세션의 소유자(Host) ID와 같은지 확인
        bool isNowHost = Activesssion.IsHost;

        // 만약 호스트 권한이 넘어왔다면 UI에 즉시 알림
        if (isNowHost)
        {
            print("방장 권한을 승계받았습니다.");
            // 내가 방장인데 현재 서버가 아니라면 (클라이언트였다면)
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                StartCoroutine(MigrateToHostRoutine());
            }
        }
    }
    IEnumerator MigrateToHostRoutine()
    {
        // 1. 기존 클라이언트 연결 종료
        NetworkManager.Singleton.Shutdown();

        // 2. 종료될 때까지 대기
        yield return new WaitUntil(() => !NetworkManager.Singleton.IsListening);
        yield return new WaitForSeconds(0.5f);

        // 3. 스스로 호스트(서버)로 시작
        NetworkManager.Singleton.StartHost();

        // 4. 서버 시작 완료 대기 후 UI 갱신
        yield return new WaitUntil(() => NetworkManager.Singleton.IsServer);

        var gameStartUI = FindFirstObjectByType<GameStart>();
        if (gameStartUI != null) gameStartUI.SetupUI();

        print("호스트 전환 및 UI 갱신 완료");
    }

    //호스트 방 생성
    public async void CreateSessionAsync(string sessionName)
    {
        try
        {
            // 1. 혹시 모를 이전 정보 제거
            if (NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();

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

            SubscribeToSessionEvents();

            GameSceneManager.Instance.LoadScene(LOBBY_SCENE_NAME);

            await System.Threading.Tasks.Task.Delay(500);
            NetworkManager.Singleton.StartHost();

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
            // 1. 전송 레이어(Transport) 강제 초기화
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                // 수동으로 주소를 비워 이전 세션과의 연결 고리를 완전히 끊음
                transport.SetConnectionData("127.0.0.1", 0);
                transport.DisconnectLocalClient();
                Debug.Log("이전 전송 레이어 데이터 초기화 완료");
            }

            // 2. NetworkManager 완전 셧다운 확인
            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
                // 셧다운이 완료될 때까지 충분히 대기
                await System.Threading.Tasks.Task.Delay(1000);
            }

            // 3. 신규 로그인 (PlayerId 강제 갱신)
            await EnsureSignedInAsync();

            var joinOptions = new QuickJoinOptions { Timeout = TimeSpan.FromSeconds(10) };
            var sessionOptions = new SessionOptions { MaxPlayers = 2 }.WithRelayNetwork();

            Activesssion = await MultiplayerService.Instance.MatchmakeSessionAsync(joinOptions, sessionOptions);

            if (Activesssion != null)
            {
                SubscribeToSessionEvents();

                // 씬 이동을 먼저 수행하여 NetworkManager가 새 씬에서 동작할 준비를 하게 함
                GameSceneManager.Instance.LoadScene(LOBBY_SCENE_NAME);

                // [매우 중요] 릴레이 서버가 이 플레이어의 '신규 ID'를 완전히 등록할 시간을 줍니다.
                // 0.5초는 너무 짧을 수 있으니 2초로 늘려보세요.
                await System.Threading.Tasks.Task.Delay(2000);

                bool success = NetworkManager.Singleton.StartClient();
                Debug.Log($"Netcode 클라이언트 시작 결과: {success}");

                updateSessionInfo?.Invoke(Activesssion.Name, Activesssion.Code);
                StartCoroutine(DelayedSetupUI());
            }
        }
        catch (Exception e)
        {
            print($"퀵 조인 실패 {e.Message}");
        }
    }

    IEnumerator DelayedSetupUI()
    {
        yield return null; // 한 프레임 대기
        var ui = FindFirstObjectByType<GameStart>();
        if (ui != null) ui.SetupUI();
    }

    //public void StartSession()
    //{
    //    NetworkManager.Singleton.SceneManager.LoadScene(GAME_SCENE_NAME, UnityEngine.SceneManagement.LoadSceneMode.Single);
    //}

    void StartSession()
    {
        //if (NetworkManager.Singleton.IsServer)
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
        //try
        //{
        //    await Activesssion.LeaveAsync();
        //    print("세션에서 나감");

        //    Activesssion = null;

        //    //로비 씬으로 이동
        //    NetworkManager.Singleton.SceneManager.LoadScene(LOBBY_SCENE_NAME, UnityEngine.SceneManagement.LoadSceneMode.Single);
        //}
        //catch (Exception e)
        //{
        //    print($"세션 나감 실패 {e.Message}");
        //}

        try
        {
            if (Activesssion != null)
            {
                // MultiplayerService를 거치지 않고 세션 객체에서 직접 호출
                await Activesssion.LeaveAsync();
                print("세션에서 퇴장했습니다.");
            }
        }
        catch (Exception e)
        {
            print($"퇴장 실패: {e.Message}");
        }
        finally
        {

            // 핵심: 네트워크 매니저를 완전히 셧다운하고 세션 변수를 비웁니다.
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            // ID 갱신을 위한
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
                print("플레이어 로그아웃 완료 (ID 초기화)");
            }

            Activesssion = null;

            // 씬 이동
            //NetworkManager.Singleton.SceneManager.LoadScene(LOBBY_SCENE_NAME, UnityEngine.SceneManagement.LoadSceneMode.Single);
            await System.Threading.Tasks.Task.Delay(1000);
            GameSceneManager.Instance.LoadScene("StartScene");
        }
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
