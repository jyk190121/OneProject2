using UnityEngine;
using UnityEngine.UI;

public class Exit : MonoBehaviour
{
    public Button exitButton;

    private void Awake()
    {
        if (exitButton == null)
            exitButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        exitButton.onClick.AddListener(OnExitClicked);
    }

    private void OnDisable()
    {
        exitButton.onClick.RemoveListener(OnExitClicked);
    }

    private void OnExitClicked()
    {
        if (MultiPlayerSessionManager.Instance != null)
        {
            print("게임 나가기 시도...");
            MultiPlayerSessionManager.Instance.LeaveSession();
        }
    }
}