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
}
