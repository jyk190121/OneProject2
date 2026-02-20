using UnityEngine;
using Unity.Netcode;

public class LobbyUIController : MonoBehaviour
{
    [Header("Player Slots")]
    public GameObject player1ActiveImage; // Player 1이 있을 때 켜질 이미지
    public GameObject player2ActiveImage; // Player 2가 들어오면 켜질 이미지

    void Update()
    {
        // 매 프레임 체크하거나, 성능을 위해 0.5초마다 체크해도 됩니다.
        UpdatePlayerSlots();
    }

    private void UpdatePlayerSlots()
    {
        if (MultiPlayerSessionManager.Instance == null) return;

        int currentPlayers = MultiPlayerSessionManager.Instance.GetPlayerCount();

        // 인원수에 따라 이미지 활성화 (모두에게 동일하게 보임)
        // 1명 이상이면 Player 1 이미지 ON
        player1ActiveImage.SetActive(currentPlayers >= 1);

        // 2명 이상이면 Player 2 이미지 ON
        player2ActiveImage.SetActive(currentPlayers >= 2);
    }
}