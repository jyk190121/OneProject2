using System.Collections.Generic;

[System.Serializable]
public class RankData
{
    public string name;
    public int score;
    public string date;
}

[System.Serializable]
public class RankingList
{
    public List<RankData> ranks = new List<RankData>();
}