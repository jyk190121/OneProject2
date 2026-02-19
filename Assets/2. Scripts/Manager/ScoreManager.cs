using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance; // 어디서든 접근 가능하게
    public TextMeshProUGUI scoreTxt;
    //int score;

    // 싱글플레이용 점수 저장 변수
    private int localScore = 0;

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

    private void UpdateScoreUI(int currentTotalScore)
    {
        if (scoreTxt != null)
        {
            scoreTxt.text = currentTotalScore.ToString();
        }
    }

    // 점수를 추가하는 통합 함수 (싱글/멀티 모두 대응)
    public void AddScoreServer(int amount)
    {
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        // [멀티플레이] 서버인 경우에만 NetworkVariable 수정
        if (isNetworkActive)
        {
            if (IsServer)
            {
                totalScore.Value += amount;
            }
        }
        // [싱글플레이] 일반 int 변수를 수정하고 즉시 UI 갱신
        else
        {
            localScore += amount;
            UpdateScoreUI(localScore);
        }
    }
}
