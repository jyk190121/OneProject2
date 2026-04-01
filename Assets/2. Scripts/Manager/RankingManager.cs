using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class RankEntry
{
    public string playerId; // 이름 대신 비교할 고유 값
    public string name;     // 표시용 이름
    public int score;
    public bool isMultiplay; // 추가: 싱글(false), 멀티(true) 구분
}

[System.Serializable]
public class RankingData
{
    public List<RankEntry> entries = new List<RankEntry>();
}

public class RankingManager : MonoBehaviour
{
    public static RankingManager Instance;
    private string savePath;
    private RankingData rankingData = new RankingData();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Path.Combine(Application.persistentDataPath, "ranking.json");
            LoadRanking();
        }
        else Destroy(gameObject);
    }

    public int GetExpectedRank(int score)
    {
        // 데이터가 아예 없으면 당연히 1등
        if (rankingData.entries == null || rankingData.entries.Count == 0) return 1;

        // 내 점수보다 높은 사람 수 + 1
        return rankingData.entries.Count(x => x.score > score) + 1;
    }

    //public void AddRank(string name, int score)
    //{
    //    rankingData.entries.Add(new RankEntry { name = name, score = score });
    //    // 점수 내림차순 정렬 후 상위 10개만 유지
    //    rankingData.entries = rankingData.entries.OrderByDescending(x => x.score).Take(10).ToList();
    //    SaveRanking();
    //}

    public void AddRank(string playerName, int score, bool isMultiplay)
    {
        string pId = PlayerIDManager.GetPlayerID(); // 고유 ID 가져오기

        // 1. 정상적인 데이터 한 개만 추가
        rankingData.entries.Add(new RankEntry
        {
            playerId = pId,
            name = playerName,
            score = score,
            isMultiplay = isMultiplay // 현재 모드 저장
        });

        // 정렬 및 개수 제한 (필터링을 위해 전체 데이터는 넉넉히 유지)
        rankingData.entries = rankingData.entries.OrderByDescending(x => x.score).Take(200).ToList();
        SaveRanking();
    }

    // 멀티용?
    public List<RankEntry> GetFilteredRanks(bool isMulti)
    {
        return rankingData.entries
            .Where(x => x.isMultiplay == isMulti) // 모드 필터링
            .OrderByDescending(x => x.score)
            .Take(99) // 화면에는 상위 99개만
            .ToList();
    }

    // 현재 점수가 최고 기록(1등)인지 확인하는 함수
    public bool IsHighScore(int score)
    {
        if (rankingData.entries.Count == 0) return true;

        // 리스트의 첫 번째(가장 높은 점수)보다 내 점수가 높으면 새로운 기록
        return score > rankingData.entries[0].score;
    }

    // 등록된 기록이 있는지 확인
    public bool HasAnyRanking()
    {
        return rankingData.entries != null && rankingData.entries.Count > 0;
    }

    private void SaveRanking()
    {
        string json = JsonUtility.ToJson(rankingData);
        File.WriteAllText(savePath, json);
    }

    private void LoadRanking()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            rankingData = JsonUtility.FromJson<RankingData>(json);
        }
    }

    //public RankEntry GetMyBestEntry(string myName)
    //{
    //    // 내 이름으로 등록된 기록들 중 가장 높은 점수 반환
    //    return rankingData.entries
    //        .Where(x => x.name == myName)
    //        .OrderByDescending(x => x.score)
    //        .FirstOrDefault();
    //}
    public RankEntry GetMyBestEntry()
    {
        if (rankingData.entries == null || rankingData.entries.Count == 0) return null;

        string pId = PlayerIDManager.GetPlayerID();

        return rankingData.entries
            .Where(x => x.playerId == pId) // 고유 ID로 필터링
            .OrderByDescending(x => x.score)
            .FirstOrDefault();
    }

    public List<RankEntry> GetTop99Ranks()
    {
        // 최대 99개까지 리스트 반환
        return rankingData.entries.Take(99).ToList();
    }


    // UI에서 리스트를 뿌려줄 때 사용
    public List<RankEntry> GetTopRanks() => rankingData.entries;
}