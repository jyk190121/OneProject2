using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using Key = UnityEngine.InputSystem.Key;
using Unity.Netcode;
public class NameController : MonoBehaviour
{
    public TMP_InputField[] nameSlots; // 3개의 InputField 연결
    public TextMeshProUGUI finalScoreTxt;
    public TextMeshProUGUI rankTxt;
    public Button submitBtn;
    GameOverManager gameOverManager;

    private void Start()
    {
        gameOverManager = FindAnyObjectByType<GameOverManager>();

        submitBtn.onClick.AddListener(OnClickSubmit);

        // 시스템적으로 한글 입력기(IME)를 비활성화 (커스텀 Input 매니저와 연동)
        UnityEngine.Input.imeCompositionMode = IMECompositionMode.Off;

        // 모든 슬롯에 이벤트 리스너 등록
        for (int i = 0; i < nameSlots.Length; i++)
        {
            int index = i;
            nameSlots[i].characterLimit = 1;

            nameSlots[i].contentType = TMP_InputField.ContentType.Custom;

            nameSlots[i].onValidateInput += (string input, int charIndex, char addedChar) => {
                return ValidateEnglishOnly(addedChar);
            };

            nameSlots[i].onValueChanged.AddListener((val) => OnValueChanged(val, index));
          
        }

        // 첫 번째 칸에 자동 포커스
        nameSlots[0].ActivateInputField();
    }

    // 영문 대/소문자만 통과시키고 나머지는 아예 무시(null 반환)
    char ValidateEnglishOnly(char addedChar)
    {
        // A-Z 또는 a-z 사이의 문자만 허용
        if ((addedChar >= 'a' && addedChar <= 'z') || (addedChar >= 'A' && addedChar <= 'Z'))
        {
            return char.ToUpper(addedChar); // 소문자도 즉시 대문자로 변환
        }

        // 그 외 모든 문자(한글, 숫자, 특수문자)는 아예 입력 안 됨
        return '\0';
    }

    private void OnValueChanged(string val, int index)
    {
        //if (string.IsNullOrEmpty(val)) return;

        // 1. 영문자만 허용 (정규식으로 한글/숫자/특수문자 즉시 제거)
        string filtered = Regex.Replace(val, @"[^a-zA-Z]", "");

        if (val != filtered)
        {
            nameSlots[index].text = filtered;
            return;
        }

        if (filtered.Length > 0)
        {
            // 2. 소문자를 대문자로 변환
            nameSlots[index].text = filtered.ToUpper();

            // 3. 다음 칸으로 이동
            if (index < nameSlots.Length - 1)
            {
                nameSlots[index + 1].ActivateInputField();
            }
        }
    }

    // Update에서 백스페이스 처리 (이전 칸으로 돌아가기)
    private void Update()
    {
        if (UnityEngine.Input.imeCompositionMode != IMECompositionMode.Off)
        {
            UnityEngine.Input.imeCompositionMode = IMECompositionMode.Off;
        }

        HandleBackspace();
    }
    void HandleBackspace()
    {
        for (int i = 1; i < nameSlots.Length; i++)
        {
            // 현재 칸이 비어있고, 백스페이스가 눌렸을 때 이전 칸으로 이동
            if (nameSlots[i].isFocused && Input.GetKeyDown(Key.Backspace) && nameSlots[i].text.Length == 0)
            {
                nameSlots[i - 1].ActivateInputField();
                nameSlots[i - 1].text = ""; // 이전 글자 삭제
            }
        }
    }


    public string GetFullName()
    {
        return nameSlots[0].text + nameSlots[1].text + nameSlots[2].text;
    }

    public void OnClickSubmit()
    {
        string fullName = GetFullName();

        // 3글자 미만 입력 시 처리 (간단한 경고나 버튼 비활성화)
        if (fullName.Length < 3)
        {
            Debug.LogWarning("이름 3글자를 모두 입력해야 합니다!");
            // UI에 "이름을 완성해주세요" 같은 텍스트를 띄워주면 더 친절합니다.
            return;
        }

        // 3글자가 완료되었다면 등록 절차 진행 (GameOverManager의 함수 호출)
        gameOverManager.OnRegisterRanking(fullName);

        GameSceneManager.Instance.LoadScene("StartScene");

        // 멀티플레이 중이었다면 세션 종료
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (MultiPlayerSessionManager.Instance != null)
            {
                MultiPlayerSessionManager.Instance.LeaveSession();
            }
        }
    }
}