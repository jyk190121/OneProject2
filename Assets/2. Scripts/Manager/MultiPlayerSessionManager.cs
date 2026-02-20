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
            // [핵심] 유니티 트랜스포트 캐시 강제 삭제
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (transport != null)
            {
                transport.SetConnectionData("0.0.0.0", 0);
                Debug.Log("이전 릴레이 캐시 초기화 완료");
            }

            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
                await System.Threading.Tasks.Task.Delay(1000);
            }

            // 로그인 새로 수행 (PlayerId 변경)
            await EnsureSignedInAsync();

            var joinOptions = new QuickJoinOptions { Timeout = TimeSpan.FromSeconds(10) };
            var sessionOptions = new SessionOptions { MaxPlayers = 2 }.WithRelayNetwork();

            Activesssion = await MultiplayerService.Instance.MatchmakeSessionAsync(joinOptions, sessionOptions);

            if (Activesssion != null)
            {
                SubscribeToSessionEvents();

                // 조인 성공 후 씬 이동
                GameSceneManager.Instance.LoadScene(LOBBY_SCENE_NAME);

                // 릴레이 서버가 새 플레이어를 인지할 시간 충분히 부여 (self-connect 방지)
                await System.Threading.Tasks.Task.Delay(1500);

                NetworkManager.Singleton.StartClient();
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
        if (NetworkManager.Singleton.IsServer)
        {
            // 중요: 씬 로드 이벤트를 구독합니다. (함수와 이벤트를 연결)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnMultiSceneLoaded;

            var status = NetworkManager.Singleton.SceneManager.LoadScene(
                GAME_SCENE_NAME,
                UnityEngine.SceneManagement.LoadSceneMode.Single
            );

            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogWarning("씬 로드 시작 실패: " + status);
                // 실패 시 이벤트 해제
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
        while (Activesssion != null)
        {
            // 2명이 찼는지 확인
            if (Activesssion.Players.Count >= 2)
            {
                print("2인 접속 확인, 3초 후 게임 시작!");
                yield return new WaitForSeconds(3f);
                StartSession();
                yield break; // 코루틴 종료
            }
            else
            {
                print("1인 플레이는 싱글플레이를 이용해주세요");
            }

            yield return new WaitForSeconds(1f); // 1초마다 확인
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
