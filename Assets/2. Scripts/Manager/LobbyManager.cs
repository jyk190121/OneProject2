using TMPro;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(LobbyManager))]
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;
    MultiPlayerSessionManager MPSessionManager => MultiPlayerSessionManager.Instance;

    public TextMeshProUGUI roomNameTxt;
    public TextMeshProUGUI codeNameTxt;

    private void Awake()
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
    }

    private void Start()
    {
        //세션매니저의 세션 코드 업데이트 이벤트 등록
        //MPSessionManager.updateSessionInfo += UpdateInfo;
        // 씬에 돌아왔을 때 만약 네트워크 매니저가 아직 '리스닝' 중이라면 강제 종료
        // 이는 비정상 종료 후 재접속 시 발생하는 에러를 막아줍니다.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        MPSessionManager.updateSessionInfo += UpdateInfo;
    }

    public void UpdateInfo(string name, string code)
    {
        if (roomNameTxt == null || codeNameTxt == null) return;
        roomNameTxt.text = name;
        codeNameTxt.text = code;

        StopAllCoroutines();
        //StartCoroutine(MPSessionManager.WaitForPlayersRoutine());
    }

    public void CheckPlayer()
    {
        StartCoroutine(MPSessionManager.WaitForPlayersRoutine());
    }

    //방 생성 후 씬이동만 (대기)
    public void CreateRoom(string roomName)
    {
        print($"{roomName} 방에 입장");
        MPSessionManager.CreateSessionAsync(roomName);
        GameSceneManager.Instance.LoadScene("Lobby");
    }

    public void JoinRoom()
    {
        MPSessionManager.QuickJoinSessionAsync();
    }

}
