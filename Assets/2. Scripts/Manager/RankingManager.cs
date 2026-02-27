using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class RankEntry
{
    public string name;
    public int score;
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
        // 내 점수보다 높은 사람 수 + 1
        return rankingData.entries.Count(x => x.score > score) + 1;
    }

    public void AddRank(string name, int score)
    {
        rankingData.entries.Add(new RankEntry { name = name, score = score });
        // 점수 내림차순 정렬 후 상위 10개만 유지
        rankingData.entries = rankingData.entries.OrderByDescending(x => x.score).Take(10).ToList();
        SaveRanking();
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

    // UI에서 리스트를 뿌려줄 때 사용
    public List<RankEntry> GetTopRanks() => rankingData.entries;
}