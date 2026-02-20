using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Services.Qos.V2.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiPlayerSessionManager : NetworkBehaviour
{
    public static MultiPlayerSessionManager Instance { get; private set; }

    ISession Activesssion { get; set; }

    const string LOBBY_SCENE_NAME = "StartScene";
    const string GAME_SCENE_NAME = "Multi";

    public event Action<string, string> updateSessionInfo;

    public GameObject playerPrefab;

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

    //호스트 방 생성
    public async void CreateSessionAsync(string sessionName)
    {
        try
        {
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
            //퀵 조인 옵션 설정
            var joinOptions = new QuickJoinOptions
            {
                //FilterField.AvailableSlots: 빈자리 체크
                //"1" : 필요한 빈자리수
                //GreaterOrEqual : 크거나 같은
                Filters = new List<FilterOption>
                {
                    new FilterOption(FilterField.AvailableSlots, "1", FilterOperation.GreaterOrEqual)
                },
                Timeout = TimeSpan.FromSeconds(5),
                CreateSession = true
            };

            //세션을 못찾았을 때 생성할 세션의 옵션 설정
            var sessionOptions = new SessionOptions
            {
                IsPrivate = false,
                IsLocked = false,
                MaxPlayers = 2
            }.WithRelayNetwork();


            //멀티플레이어서비스한테 매치메이킹 요청
            Activesssion = await MultiplayerService.Instance.MatchmakeSessionAsync(joinOptions, sessionOptions);

            print($"세션 참가 완료 / 코드 {Activesssion.Code}");
            print($"현재 플레이어 수 :{Activesssion.Players.Count} / {Activesssion.MaxPlayers}");

            updateSessionInfo?.Invoke(Activesssion.Name, Activesssion.Code);
        }
        catch (Exception e)
        {
            print($"퀵 조인 실패 {e.Message}");
        }
    }

    //public void StartSession()
    //{
    //    NetworkManager.Singleton.SceneManager.LoadScene(GAME_SCENE_NAME, UnityEngine.SceneManagement.LoadSceneMode.Single);
    //}

    public void StartSession()
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
        try
        {
            await Activesssion.LeaveAsync();
            print("세션에서 나감");

            Activesssion = null;

            //로비 씬으로 이동
            NetworkManager.Singleton.SceneManager.LoadScene(LOBBY_SCENE_NAME, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            print($"세션 나감 실패 {e.Message}");
        }
    }

    //// 세션 객체에 플레이어 참가 이벤트 연결
    //void SubscribeToSessionEvents()
    //{
    //    if (Activesssion == null) return;

    //    // 플레이어 목록이 갱신될 때마다 호출되는 이벤트 (SDK 버전에 따라 다를 수 있음)
    //    // 만약 이벤트가 없다면 코루틴으로 인원수를 체크해야 합니다.
    //    Activesssion.PlayerJoined += (player) => {
    //        CheckFullAndStart();
    //    };
    //}

    //void CheckFullAndStart()
    //{
    //    // 호스트만 씬 로드를 제어해야 함
    //    if (NetworkManager.Singleton.IsServer && Activesssion.Players.Count >= Activesssion.MaxPlayers)
    //    {
    //        print("모든 플레이어 입장 완료! 씬을 시작합니다.");
    //        StartSession(); // 기존에 만든 씬 로드 함수 호출
    //    }
    //}

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

            yield return new WaitForSeconds(1f); // 1초마다 확인
        }
    }
    void OnMultiSceneLoaded(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName == GAME_SCENE_NAME && IsServer)
        {
            // 이벤트가 중복 실행되지 않도록 한 번 실행 후 바로 해제
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnMultiSceneLoaded;

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
}
