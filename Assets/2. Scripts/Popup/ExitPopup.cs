using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
public class ExitPopup : BasePopup
{
    public GameObject popupPanel;
    public TextMeshProUGUI messageTxt;
    public Button yesBtn;
    public Button noBtn;

    private Action onYesAction; // Yes 버튼 클릭 시 실행할 동작 저장

    void OnEnable()
    {
        // 버튼 이벤트 연결
        yesBtn.onClick.AddListener(OnYesClicked);
        noBtn.onClick.AddListener(() => OnNoClicked());
    }

    void OnDisable()
    {
        yesBtn.onClick.RemoveListener(OnYesClicked);
        noBtn.onClick.RemoveListener(() => OnNoClicked());
    }

    // 외부에서 호출할 함수: 메시지와 실행할 액션을 전달받음
    public void ShowConfirm(string message, Action confirmAction)
    {
        base.Show(() => { /* 기본 닫기 외 추가 로직 필요시 */ });
        // UI Toolkit 버튼 이벤트 연결 등...
        // root.Q<Button>("CloseBtn").clicked += () => Close();

        messageTxt.text = message;
        onYesAction = confirmAction;
        popupPanel.SetActive(true);
    }

    private void OnYesClicked()
    {
        onYesAction?.Invoke(); // 저장된 액션 실행
        //popupPanel.SetActive(false);
    }

    private void OnNoClicked()
    {
        base.Close();
        popupPanel.SetActive(false);
    }
}
