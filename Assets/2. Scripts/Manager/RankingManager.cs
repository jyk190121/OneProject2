using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class RankingManager : MonoBehaviour
{
    public static RankingManager Instance;
    private string savePath;
    private RankingList rankingList = new RankingList();

    void Awake()
    {
        Instance = this;
        savePath = Path.Combine(Application.persistentDataPath, "ranking.json");
        LoadRanking();
    }

    public void AddRank(string playerName, int playerScore)
    {
        RankData newRank = new RankData
        {
            name = playerName,
            score = playerScore,
            date = System.DateTime.Now.ToString("yyyy-MM-dd")
        };

        rankingList.ranks.Add(newRank);
        // 점수 내림차순 정렬 후 상위 10개만 유지
        rankingList.ranks = rankingList.ranks.OrderByDescending(r => r.score).Take(10).ToList();

        SaveRanking();
    }

    public void SaveRanking()
    {
        string json = JsonUtility.ToJson(rankingList);
        File.WriteAllText(savePath, json);
    }

    public void LoadRanking()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            rankingList = JsonUtility.FromJson<RankingList>(json);
        }
    }

    public List<RankData> GetRanks() => rankingList.ranks;
}