//using System;
//using System.Collections.Generic;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;
//public class RankingPopup : BasePopup
//{
//    public GameObject popupPanel;
//    public TextMeshProUGUI messageTxt;
//    //public Button yesBtn;
//    public Button noBtn;

//    [Header("UI References")]
//    public GameObject entryPrefab;    // 순위/이름/점수가 포함된 줄(Row) 프리팹
//    public Transform contentParent;   // ScrollView의 Content
//    public TextMeshProUGUI myBestTxt; // 하단 "내 점수 : AAA 0000" 텍스트

//    private Action onYesAction; // Yes 버튼 클릭 시 실행할 동작 저장

//    void Awake()
//    {
//        popupPanel.SetActive(false);

//        // 버튼 이벤트 연결
//        //yesBtn.onClick.AddListener(OnYesClicked);
//        noBtn.onClick.AddListener(() => OnNoClicked());
//    }

//    // 외부에서 호출할 함수: 메시지와 실행할 액션을 전달받음
//    public void ShowConfirm(string message, Action confirmAction)
//    {
//        messageTxt.text = message;
//        onYesAction = confirmAction;
//        popupPanel.SetActive(true);
//    }

//    private void OnYesClicked()
//    {
//        onYesAction?.Invoke(); // 저장된 액션 실행
//        popupPanel.SetActive(false);
//    }

//    private void OnNoClicked()
//    {
//        popupPanel.SetActive(false);
//    }

//    // 팝업이 활성화될 때 실행 (버튼 클릭 시 SetActive(true))
//    private void OnEnable()
//    {
//        RefreshRankingList();
//    }

//    public void RefreshRankingList()
//    {
//        // 1. 기존 리스트 아이템 삭제 (풀링을 쓰지 않는 경우)
//        foreach (Transform child in contentParent)
//        {
//            Destroy(child.gameObject);
//        }

//        if (RankingManager.Instance == null) return;

//        // 2. 99위까지 리스트 생성
//        List<RankEntry> ranks = RankingManager.Instance.GetTop99Ranks();
//        for (int i = 0; i < ranks.Count; i++)
//        {
//            GameObject go = Instantiate(entryPrefab, contentParent);
//            // 텍스트 구성: "01  AAA    5000" (간격은 폰트에 맞춰 조정)
//            var rowText = go.GetComponentInChildren<TextMeshProUGUI>();
//            rowText.text = $"{(i + 1):D2}  {ranks[i].name}    {ranks[i].score:N0}";
//        }

//        // 3. 내 최고 점수 표시
//        string lastUsedName = PlayerPrefs.GetString("LastPlayerName", "");
//        if (!string.IsNullOrEmpty(lastUsedName))
//        {
//            var bestEntry = RankingManager.Instance.GetMyBestEntry(lastUsedName);
//            if (bestEntry != null)
//                myBestTxt.text = $"내 최고점수 : {bestEntry.name}    {bestEntry.score:N0}";
//            else
//                myBestTxt.text = "내 최고점수 : --- ----";
//        }
//        else
//        {
//            myBestTxt.text = "내 최고점수 : 기록 없음";
//        }
//    }

//    public void ClosePopup()
//    {
//        gameObject.SetActive(false);
//    }
//}


using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankingPopup : BasePopup
{
    public GameObject popupPanel;
    public Button noBtn; // 닫기 버튼

    [Header("Ranking UI References")]
    public Transform contentParent;   // ScrollView의 Content (Vertical Layout Group 필수)
    public TextMeshProUGUI myBestTxt;
    public TMP_FontAsset arcadeFont;

    void OnEnable()
    {
        // 버튼 이벤트 연결 (ExitPopup 패턴 적용)
        noBtn.onClick.AddListener(OnNoClicked);

        // 팝업이 켜질 때 리스트 갱신
        RefreshRankingList();
    }

    void OnDisable()
    {
        // 이벤트 해제
        noBtn.onClick.RemoveListener(OnNoClicked);
    }

    // StartScene의 버튼에서 호출할 진입점
    public void ShowRanking()
    {
        // 1. BasePopup의 Show 호출 (StartScene의 UIDocument 숨김 등)
        base.Show(() => { Debug.Log("랭킹 팝업 닫힘"); });

        // 2. UGUI 패널 활성화
        popupPanel.SetActive(true);
    }

    private void OnNoClicked()
    {
        // 1. BasePopup의 Close 호출 (StartScene UI 복구)
        base.Close();

        // 2. UGUI 패널 비활성화
        popupPanel.SetActive(false);
    }

    public void RefreshRankingList()
    {
        // 기존 리스트 삭제
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        if (RankingManager.Instance == null) return;

        List<RankEntry> ranks = RankingManager.Instance.GetTop99Ranks();

        for (int i = 0; i < ranks.Count; i++)
        {
            // 한 줄(Row) 생성
            GameObject rowObj = new GameObject($"Rank_{i + 1}", typeof(RectTransform));
            rowObj.transform.SetParent(contentParent, false);

            // 높이 지정
            RectTransform rowRect = rowObj.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0, 50);

            // 텍스트 생성
            CreateRankText(rowObj.transform, i + 1, ranks[i].name, ranks[i].score);
        }

        UpdateMyBestScore();
    }

    private void CreateRankText(Transform parent, int rank, string pName, int pScore)
    {
        GameObject textObj = new GameObject("TextData", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.font = arcadeFont;
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Left;

        // Rich Text를 활용한 정렬: [순위] [이름] [점수(우측)]
        // <pos=비율> 태그로 탭 간격을 맞춥니다.
        tmp.text = $"{rank:D2}  {pName} <pos=60%>{pScore:N0}";

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(30, 0);
        rect.offsetMax = new Vector2(-30, 0);
    }

    private void UpdateMyBestScore()
    {
        string lastUsedName = PlayerPrefs.GetString("LastPlayerName", "");
        if (!string.IsNullOrEmpty(lastUsedName))
        {
            var bestEntry = RankingManager.Instance.GetMyBestEntry(lastUsedName);
            if (bestEntry != null)
                myBestTxt.text = $"내 최고점수 : {bestEntry.name}    {bestEntry.score:N0}";
            else
                myBestTxt.text = "내 최고점수 : --- 0000";
        }
        else
        {
            myBestTxt.text = "내 최고점수 : 기록 없음";
        }
    }
}