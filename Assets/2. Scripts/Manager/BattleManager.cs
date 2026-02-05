using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance; // 어디서든 접근 가능하게
    public List<JoystickPlayer> joystickPlayers = new List<JoystickPlayer>();
    public bool isStarting;
    public Image countImg;
    public TextMeshProUGUI countTxt;
    List<Enemy> enemies;
    Player player;
    public VariableJoystick joystick;
    public Image playerHP_bar;

    public GameObject box_A;       // 떨어지는 블록 (밀림)
    public GameObject box_B;       // 떨어지는 블록 (밀림)
    public GameObject wallB;       // Y축 고정 블록 (안밀림)

    GameObject[] block = new GameObject[3];

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            DontDestroyOnLoad(Instance);
        }
        block[0] = box_A;
        block[1] = box_B;
        block[2] = wallB;
    }

    public void RegisterPlayer(JoystickPlayer player)
    {
        if (!joystickPlayers.Contains(player))
        {
            joystickPlayers.Add(player);
            
            if(player.variableJoystick == null)
            {
                player.variableJoystick = joystick;
            }
            if(player.HP_BAR == null)
            {
                player.HP_BAR = playerHP_bar;
            }

        }
    }

    void Start()
    {
        countImg.gameObject.SetActive(true);
        StartCoroutine(StartDelayRoutine());
        //StartCoroutine(StartBlockCreate());
        // [수정] Update 대신 Start에서 한 번만 호출하여 반복 루프를 돌립니다.
        StartCoroutine(BlockSpawnLoop());
    }
    IEnumerator StartDelayRoutine()
    {
        isStarting = true;

        countTxt.text = "3";
        yield return new WaitForSeconds(1f);
        countTxt.text = "2";
        yield return new WaitForSeconds(1f);
        countTxt.text = "1";
        yield return new WaitForSeconds(1f);

        countTxt.color = Color.blue;
        countTxt.text = "GO";
        isStarting = false;

        yield return new WaitForSeconds(1f);
        // countLabel.style.display = DisplayStyle.None; // UI 숨기기
        countImg.gameObject.SetActive(false);
        countTxt.text = "";
    }

    //IEnumerator StartBlockCreate()
    //{
    //    int r = Random.Range(0, 2);
    //    int ea = Random.Range(0, 10);

    //    yield return new WaitForSeconds(3f);

    //    StartCoroutine(CreateBlcok(r, ea));

    //    yield return new WaitForSeconds(15f);
    //}

    //IEnumerator CreateBlcok(int r, int ea)
    //{
    //    for (int i = 0; i < ea; i++)
    //    {
    //        float clampX = Random.Range(-6f, 6.1f);
    //        float clampZ = Random.Range(-45f, 45.1f);
    //        float y = -0.5f;

    //        if (r == 0) y = Mathf.Clamp(y, 0f, 15f);

    //        yield return new WaitForSeconds(3f);

    //        transPos.position = new Vector3(clampX, y, clampZ);

    //        yield return new WaitForSeconds(3f);
            
    //        Instantiate(block[r], transPos);
    //    }
    //}

    // [추가] 블록 생성을 주기적으로 반복하는 루프
    IEnumerator BlockSpawnLoop()
    {
        while (true) // 게임이 끝날 때까지 반복
        {
            yield return new WaitForSeconds(5f); // 15초마다 블록 생성 파동 시작

            int r = Random.Range(0, 3);

            int ea = Random.Range(1, 10);

            yield return StartCoroutine(CreateBlock(r, ea));
        }
    }

    IEnumerator CreateBlock(int r, int ea)
    {
        for (int i = 0; i < ea; i++)
        {
            float clampX = Random.Range(-40f, 40.1f);
            float clampZ = Random.Range(-45f, 45.1f);
            float y = (r != 2) ? Random.Range(0f, 15f) : -0.5f;

            Vector3 spawnPosition = new Vector3(clampX, y, clampZ);

            // [해결] transPos를 거치지 않고 직접 위치를 지정하여 생성
            // Quaternion.identity는 회전값 없음(0,0,0)을 의미합니다.
            GameObject newBlock = Instantiate(block[r], transform);
            newBlock.transform.position = spawnPosition;

            yield return new WaitForSeconds(1f); // 블록 하나 생성 후 대기 시간
        }
    }
}
