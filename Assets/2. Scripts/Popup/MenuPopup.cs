using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
public class MenuPopup : BasePopup
{
    public GameObject popupPanel;
    public TextMeshProUGUI messageTxt;
    public TMP_InputField roomName;
    public Button createBtn;
    public Button joinBtn;
    public Button noBtn;

    private Action onYesAction; // Yes 버튼 클릭 시 실행할 동작 저장

    private void OnEnable()
    {
        // 버튼 이벤트 연결
        createBtn.onClick.AddListener(OnCreateClicked);
        joinBtn.onClick.AddListener(OnJoinClicked);
        noBtn.onClick.AddListener(() => OnNoClicked());
    }

    private void OnDisable()
    {
        createBtn.onClick.RemoveListener(OnCreateClicked);
        joinBtn.onClick.RemoveListener(OnJoinClicked);
        noBtn.onClick.RemoveListener(() => OnNoClicked());
    }

    public void ShowConfirm(string message, Action confirmAction)
    {
        base.Show(() => { /* 기본 닫기 외 추가 로직 필요시 */ });
        // UI Toolkit 버튼 이벤트 연결 등...
        // root.Q<Button>("CloseBtn").clicked += () => Close();

        messageTxt.text = message;
        onYesAction = confirmAction;
        popupPanel.SetActive(true);
    }

    private void OnCreateClicked()
    {
        if (roomName.text == null || roomName.text == "")
        {
            print("방 이름을 입력해주세요");
            return;
        }

        onYesAction?.Invoke(); // 저장된 액션 실행
        popupPanel.SetActive(false);
    }

    void OnJoinClicked()
    {
        LobbyManager.Instance.JoinRoom();
    }

    private void OnNoClicked()
    {
        base.Close();
        popupPanel.SetActive(false);
    }
}
