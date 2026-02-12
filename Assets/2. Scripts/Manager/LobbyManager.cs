using TMPro;
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
        MPSessionManager.updateSessionInfo += UpdateInfo;
    }

    public void UpdateInfo(string name, string code)
    {
        if (roomNameTxt == null || codeNameTxt == null) return;
        roomNameTxt.text = name;
        codeNameTxt.text = code;
    }

    public void CreateRoom(string roomName)
    {
        print($"{roomName} 방에 입장");
        MPSessionManager.CreateSessionAsync(roomName);
        GameSceneManager.Instance.LoadScene("Map");
    }

    public void JoinRoom()
    {
        print("방에 입장");
        MPSessionManager.QuickJoinSessionAsync();
    }

    public void GameStart()
    {
        MPSessionManager.StartSession();
    }
}
