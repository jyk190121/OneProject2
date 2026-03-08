using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public NameController nameController;
    public TextMeshProUGUI scoreDisplay;
    public TextMeshProUGUI rankDisplay;
    public Button submitBtn;

    private int finalScore = 0;

    void Start()
    {
        //UpdateScoreDisplay();

        //// 1. 점수 불러오기 (ScoreManager가 DontDestroyOnLoad라고 가정)
        //int score = 0;
        //if (ScoreManager.Instance != null)
        //{
        //    //score = ScoreManager.Instance.GetFinalScore(); // 현재 씬의 점수 값 가져오기
        //}

        //scoreDisplay.text = score.ToString();

        //// 2. 임시 랭킹 계산 (등록 전 현재 점수로 몇 위인지 미리 보여줌)
        //if (RankingManager.Instance != null)
        //{
        //    int expectedRank = RankingManager.Instance.GetExpectedRank(score);
        //    rankDisplay.text = expectedRank.ToString();
        //}

        if (ScoreManager.Instance != null)
        {
            finalScore = ScoreManager.Instance.GetFinalScore();
        }

        scoreDisplay.text = finalScore.ToString("N0");

        if (RankingManager.Instance != null)
        {
            // 1. 예상 등수 계산 (기본적으로 데이터 없으면 1이 나옴)
            int expectedRank = RankingManager.Instance.GetExpectedRank(finalScore);
            rankDisplay.text = expectedRank.ToString();

            // 2. 랭킹 데이터 존재 여부 및 비교 로그 (개발용)
            if (!RankingManager.Instance.HasAnyRanking())
            {
                Debug.Log("최초 등록자입니다! 무조건 1등!");
            }
            else if (RankingManager.Instance.IsHighScore(finalScore))
            {
                Debug.Log("축하합니다! 새로운 최고 기록입니다.");
                // 여기서 rankDisplay의 색상을 금색으로 바꾸는 등의 연출을 추가할 수 있습니다.
                rankDisplay.color = new Color(1f, 0.84f, 0f); // Gold
            }
        }

        submitBtn.onClick.AddListener(OnClickConfirm);
    }
    //void UpdateScoreDisplay()
    //{
    //    if (ScoreManager.Instance != null)
    //    {
    //        // 1. 멀티플레이 여부 확인
    //        bool isMultiplay = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    //        if (isMultiplay)
    //        {
    //            // 멀티일 때는 NetworkVariable인 totalScore 사용
    //            finalScore = ScoreManager.Instance.totalScore.Value;
    //        }
    //        else
    //        {
    //            // 싱글일 때는 일반 변수인 localScore 사용 (ScoreManager에 public int localScore가 있다고 가정)
    //            finalScore = ScoreManager.Instance.localScore;
    //        }
    //    }

    //    scoreDisplay.text = $"점수 : {finalScore:N0}";

    //    // 2. 현재 점수로 예상 등수 표시
    //    if (RankingManager.Instance != null)
    //    {
    //        int expectedRank = RankingManager.Instance.GetExpectedRank(finalScore);
    //        rankDisplay.text = $"랭킹 : {expectedRank}";
    //    }
    //}
    // 등록 버튼(SubmitBtn)에 연결할 함수
    public void OnClickConfirm()
    {
        string finalName = nameController.GetFullName();

        // 3글자 검증
        if (finalName.Length < 3)
        {
            Debug.Log("이름을 3글자 입력해주세요.");
            return;
        }

        // 이름 기억 (나중에 최고점수 조회용)
        PlayerPrefs.SetString("LastPlayerName", finalName);

        // 랭킹 저장
        if (RankingManager.Instance != null)
        {
            RankingManager.Instance.AddRank(finalName, finalScore);
        }

        // 저장 후 메인 씬으로 이동 또는 랭킹판 표시
        GameSceneManager.Instance.LoadScene("StartScene");

        // 멀티플레이 중이었다면 세션 종료
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (MultiPlayerSessionManager.Instance != null)
                MultiPlayerSessionManager.Instance.LeaveSession();
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

    //public void OnClickConfirm()
    //{
    //    string finalName = nameController.GetFullName();
    //    if (finalName.Length < 3) return;

    //    // 랭킹 저장 로직 실행
    //    int score = int.Parse(scoreDisplay.text.Replace("점수 : ", ""));
    //    RankingManager.Instance.AddRank(finalName, score);

    //    // 저장 후 랭킹보드 씬으로 이동하거나 전체 순위판 띄우기
    //}


}