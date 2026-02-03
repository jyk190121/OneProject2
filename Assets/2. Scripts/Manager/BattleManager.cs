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

    public GameObject wallA;       // 떨어지는 블록 (밀림)
    public GameObject wallB;       // Y축 고정 블록 (안밀림)

    GameObject[] block = new GameObject[2];
    Transform transPos;

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
        block[0] = wallA;
        block[1] = wallB;
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
    }

    void Update()
    {
        StartCoroutine(StartBlockCreate());
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

    IEnumerator StartBlockCreate()
    {
        int r = Random.Range(0, 2);
        int ea = Random.Range(0, 10);

        yield return new WaitForSeconds(3f);

        CreateBlcok(r, ea);

        yield return new WaitForSeconds(15f);
    }

    void CreateBlcok(int r, int ea)
    {
        for (int i = 0; i < ea; i++)
        {
            float clampX = Random.Range(-6f, 6.1f);
            float clampZ = Random.Range(-45f, 45.1f);
            float y = -0.5f;

            if (r == 0) y = Mathf.Clamp(y, 0f, 15f);

            transPos.position = new Vector3(clampX, y, clampZ);

            Instantiate(block[r], transPos);
        }
    }
}
