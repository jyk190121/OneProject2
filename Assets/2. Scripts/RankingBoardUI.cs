using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class RankingBoardUI : MonoBehaviour
{
    public GameObject rankEntryPrefab; // 알파벳 3칸 + 점수가 포함된 프리팹
    public Transform contentParent;    // ScrollView의 Content
    public TextMeshProUGUI myBestScoreTxt; // "내 점수 : AAA 5000" 표시용

    void OnEnable()
    {
        RefreshBoard();
    }

    public void RefreshBoard()
    {
        // 1. 기존 리스트 UI 삭제
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        if (RankingManager.Instance == null) return;

        // 2. 99위까지 리스트 생성
        List<RankEntry> topRanks = RankingManager.Instance.GetTop99Ranks();
        for (int i = 0; i < topRanks.Count; i++)
        {
            GameObject entry = Instantiate(rankEntryPrefab, contentParent);
            // 프리팹 내부 텍스트 설정 (예: "01  AAA    1234")
            var txt = entry.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = $"{(i + 1):D2}  {topRanks[i].name}    {topRanks[i].score:N0}";
        }

        // 3. 나의 최고 점수 표시 (최근 입력한 이름 기준)
        // GameOverManager에서 저장 시 사용한 이름을 PlayerPrefs 등에 임시 저장해두면 편리합니다.
        string lastUsedName = PlayerPrefs.GetString("LastPlayerName", "???");
        var best = RankingManager.Instance.GetMyBestEntry(lastUsedName);

        if (best != null)
            myBestScoreTxt.text = $"내 최고점수 : {best.name}    {best.score:N0}";
        else
            myBestScoreTxt.text = "내 최고점수 : 없음";
    }
}