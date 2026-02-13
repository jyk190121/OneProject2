using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class MultiPlayerSessionManager : MonoBehaviour
{
    public static MultiPlayerSessionManager Instance { get; private set; }

    ISession Activesssion { get; set; }

    const string LOBBY_SCENE_NAME = "LobbyScene";
    const string GAME_SCENE_NAME = "GameScene";

    public event Action<string, string> updateSessionInfo;

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
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            print($"익명 로그인 성공 {AuthenticationService.Instance.PlayerId}");
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
                //Name = LOBBY_SCENE_NAME,
                Name = sessionName,
                IsPrivate = false,
                IsLocked = false,       //true인 경우 게임 시작 후 새로운 플레이어 참가불가
                MaxPlayers = 4
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
                MaxPlayers = 4
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

    public void StartSession()
    {
        NetworkManager.Singleton.SceneManager.LoadScene(GAME_SCENE_NAME, UnityEngine.SceneManagement.LoadSceneMode.Single);
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
}
