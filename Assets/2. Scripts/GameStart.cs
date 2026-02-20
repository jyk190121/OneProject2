using UnityEngine;
using UnityEngine.UI;

public class GameStart : MonoBehaviour
{
    public Button startBtn;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        startBtn.onClick.AddListener(MultiGameStart);
    }

    void MultiGameStart()
    {
        //GameSceneManager.Instance.LoadScene("Multi");
        LobbyManager.Instance.GameStart();
    }

    private void OnDisable()
    {
        startBtn.onClick.RemoveListener(MultiGameStart);
    }
}
