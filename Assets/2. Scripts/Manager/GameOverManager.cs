using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    public NameController nameController;
    public TextMeshProUGUI scoreDisplay;
    public TextMeshProUGUI rankDisplay;

    private int finalScore = 0;

    void Start()
    {
        UpdateScoreDisplay();

        // 1. 점수 불러오기 (ScoreManager가 DontDestroyOnLoad라고 가정)
        int score = 0;
        if (ScoreManager.Instance != null)
        {
            //score = ScoreManager.Instance.GetFinalScore(); // 현재 씬의 점수 값 가져오기
        }

        scoreDisplay.text = score.ToString();

        // 2. 임시 랭킹 계산 (등록 전 현재 점수로 몇 위인지 미리 보여줌)
        if (RankingManager.Instance != null)
        {
            int expectedRank = RankingManager.Instance.GetExpectedRank(score);
            rankDisplay.text = expectedRank.ToString();
        }
    }
    void UpdateScoreDisplay()
    {
        if (ScoreManager.Instance != null)
        {
            // 1. 멀티플레이 여부 확인
            bool isMultiplay = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

            if (isMultiplay)
            {
                // 멀티일 때는 NetworkVariable인 totalScore 사용
                finalScore = ScoreManager.Instance.totalScore.Value;
            }
            else
            {
                // 싱글일 때는 일반 변수인 localScore 사용 (ScoreManager에 public int localScore가 있다고 가정)
                finalScore = ScoreManager.Instance.localScore;
            }
        }

        scoreDisplay.text = $"점수 : {finalScore:N0}";

        // 2. 현재 점수로 예상 등수 표시
        if (RankingManager.Instance != null)
        {
            int expectedRank = RankingManager.Instance.GetExpectedRank(finalScore);
            rankDisplay.text = $"랭킹 : {expectedRank}";
        }
    }

    // NameController에서 호출할 등록 함수
    public void OnRegisterRanking(string playerName)
    {
        if (RankingManager.Instance != null)
        {
            RankingManager.Instance.AddRank(playerName, finalScore);
            Debug.Log($"랭킹 등록 완료: {playerName} - {finalScore}점");

            // 등록 후 처리: 버튼 비활성화나 랭킹보드 씬 이동 등
            // SceneManager.LoadScene("RankingBoardScene");
        }
    }

    public void OnClickConfirm()
    {
        string finalName = nameController.GetFullName();
        if (finalName.Length < 3) return;

        // 랭킹 저장 로직 실행
        int score = int.Parse(scoreDisplay.text.Replace("점수 : ", ""));
        RankingManager.Instance.AddRank(finalName, score);

        // 저장 후 랭킹보드 씬으로 이동하거나 전체 순위판 띄우기
    }
}