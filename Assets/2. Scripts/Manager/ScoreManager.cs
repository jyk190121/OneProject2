using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance; // 어디서든 접근 가능하게
    public TextMeshProUGUI scoreTxt;
    int score;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            DontDestroyOnLoad(Instance);
        }
    }
    void Start()
    {
        score = 0;
        scoreTxt.text = "0";
    }

    public void ScoreUpdateUI(int score)
    {
        this.score += score;
        scoreTxt.text = this.score.ToString();
    }
    
}
