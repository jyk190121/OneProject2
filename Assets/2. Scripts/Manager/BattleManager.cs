using AnimatedBattleText.Examples;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance; // 어디서든 접근 가능하게
    public List<JoystickPlayer> joystickPlayers = new List<JoystickPlayer>();
    public bool isStarting;
    public Image countImg;
    public TextMeshProUGUI countTxt;

    void Awake() => Instance = this;

    public void RegisterPlayer(JoystickPlayer player)
    {
        if (!joystickPlayers.Contains(player))
        {
            joystickPlayers.Add(player);
        }
    }

    void Start()
    {
        countImg.gameObject.SetActive(true);
        StartCoroutine(StartDelayRoutine());
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
}
