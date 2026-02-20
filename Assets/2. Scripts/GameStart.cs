using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameStart : MonoBehaviour
{
    public Button startBtn;
    //void Start()
    //{
    //    if (NetworkManager.Singleton != null)
    //    {
    //        // 서버(호스트)인 경우에만 버튼을 보여줌
    //        bool isHost = NetworkManager.Singleton.IsServer;
    //        startBtn.gameObject.SetActive(isHost);

    //        print($"UI 셋업 완료: 호스트 여부 = {isHost}");
    //    }
    //}
    private void Start()
    {
        // 세션 매니저로부터 호스트 변경 이벤트를 구독합니다.
        if (MultiPlayerSessionManager.Instance != null)
        {
            MultiPlayerSessionManager.Instance.onHostChanged += HandleHostChanged;
        }
    }

    void HandleHostChanged(bool isNowHost)
    {
        // 내가 새로운 호스트가 되었다면 시작 버튼을 켜줌
        if (startBtn != null)
        {
            startBtn.gameObject.SetActive(isNowHost);
            Debug.Log(isNowHost ? "방장이 되었습니다! 시작 버튼 활성" : "방장 권한 상실");
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        startBtn.onClick.AddListener(MultiGameStart);
    }

    void MultiGameStart()
    {
        //GameSceneManager.Instance.LoadScene("Multi");
        LobbyManager.Instance.CheckPlayer();
    }

    private void OnDisable()
    {
        startBtn.onClick.RemoveListener(MultiGameStart);
    }

    public void SetupUI()
    {
        if (NetworkManager.Singleton != null)
        {
            // 서버(호스트)인 경우에만 버튼을 보여줌
            bool isHost = NetworkManager.Singleton.IsServer;
            startBtn.gameObject.SetActive(isHost);

            print($"UI 셋업 완료: 호스트 여부 = {isHost}");
        }
    }

    void OnDestroy()
    {
        if (MultiPlayerSessionManager.Instance != null)
            MultiPlayerSessionManager.Instance.onHostChanged -= HandleHostChanged;
    }
}
