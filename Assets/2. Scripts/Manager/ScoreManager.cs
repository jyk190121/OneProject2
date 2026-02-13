using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance; // 어디서든 접근 가능하게
    public TextMeshProUGUI scoreTxt;
    //int score;

    public NetworkVariable<int> totalScore = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public override void OnNetworkSpawn()
    {
        //// 점수가 변할 때마다 UI를 업데이트하도록 이벤트 등록
        //totalScore.OnValueChanged += (oldValue, newValue) => {
        //    ScoreUpdateUI(newValue);
        //};

        // 1. 스폰 시점에 현재 서버에 저장된 점수로 UI 초기화
        UpdateScoreUI(totalScore.Value);

        // 2. 점수가 변할 때마다 UI를 업데이트하도록 이벤트 등록
        totalScore.OnValueChanged += (oldValue, newValue) => {
            UpdateScoreUI(newValue);
        };
    }

    // [중요] 이 함수는 누적된 '최종 점수'를 받아서 글자만 바꿔줍니다.
    private void UpdateScoreUI(int currentTotalScore)
    {
        if (scoreTxt != null)
        {
            scoreTxt.text = currentTotalScore.ToString();
        }
    }

    // 서버에서만 실행되는 점수 추가 함수
    public void AddScoreServer(int amount)
    {
        if (!IsServer) return;
        totalScore.Value += amount;
    }
    //void Start()
    //{
    //    score = 0;
    //    scoreTxt.text = "0";
    //}

    //void ScoreUpdateUI(int score)
    //{
    //    this.score += score;
    //    scoreTxt.text = this.score.ToString();
    //}

    //public void AddScoreServer(int amount)
    //{
    //    if (!IsServer) return;
    //    totalScore.Value += amount;
    //}

}
