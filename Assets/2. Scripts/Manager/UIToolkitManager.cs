using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class UIToolkitManager : MonoBehaviour
{
    UIDocument document;
    VisualElement panel;

    Button startBtn;
    Button menuBtn;
    Button rankingBtn;
    Button soundBtn;
    Button settingBtn;
    Button exitBtn;
    MenuPopup menu;
    RankingPopup rank;
    SettingPopup setting;
    ExitPopup exit;

    [Header("Fade In 시간")]
    float fadeDuration = 1.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menu = GetComponent<MenuPopup>();
        rank = GetComponent<RankingPopup>();
        setting = GetComponent<SettingPopup>();
        exit = GetComponent<ExitPopup>();

        document = GetComponent<UIDocument>();
        panel = document.rootVisualElement;

        menuBtn = panel.Q<Button>("MenuBtn");
        startBtn = panel.Q<Button>("PlayBtn");
        rankingBtn = panel.Q<Button>("RankingBtn");
        soundBtn = panel.Q<Button>("SoundBtn");
        settingBtn = panel.Q<Button>("SettingBtn");
        exitBtn = panel.Q<Button>("ExitBtn");

        //UI캔버스 창 띄우기
        //멀티 -> 코드 입력 창, 닫기
        menuBtn.clickable.clicked += MultiPlay;
        //싱글 플레이
        startBtn.clickable.clicked += GameStart;
        //랭킹보드
        rankingBtn.clickable.clicked += RankingBoard;
        //사운드 모두 끄기 / 켜기
        soundBtn.clickable.clicked += SoundMuteSetting;
        //사운드 조절 창 / 내 캐릭터 정보 (선택)
        settingBtn.clickable.clicked += SettingInfo;
        //게임 종료 여부 창
        exitBtn.clickable.clicked += GameExit;

        //스타트 씬 Fadein 효과
        StartCoroutine(FadeInUi());
    }

    IEnumerator FadeInUi()
    {
        //시작 시 UI의 투명도를 0으로 설정
        panel.style.opacity = 0;

        yield return null;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            panel.style.opacity = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        panel.style.opacity = 1;
    }

    void MultiPlay()
    {
        menu.ShowConfirm("방 이름을 입력해주세요", MoveLobby);
    }

    public void MoveLobby()
    {
        //print($"로비 창에 입장하셨습니다 {menu.roomName.text}");
        //LobbyManager쪽에 menu.roomName만 넘겨주자
        LobbyManager.Instance.CreateRoom(menu.roomName.text);
    }

    void GameStart()
    {
        GameSceneManager.Instance.LoadScene("Single");
    }

    void RankingBoard()
    {
        rank.ShowRanking();
    }

    void SoundMuteSetting()
    {

    }

    void SettingInfo()
    {

    }

    void GameExit()
    {
        //Application.Quit();
        exit.ShowConfirm("게임을 종료하시겠습니까", Quit);
    }

    void Quit()
    {
        // 1. 필요한 데이터 저장 (예: 점수 저장 등)
        // SaveGameData();

        // 2. 네트워크 연결 해제
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        // 3. 완전 종료
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif

    }
}
