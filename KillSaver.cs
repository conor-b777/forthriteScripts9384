using UnityEngine;
using TMPro;

public class KillSaver : MonoBehaviour
{
    private PlayerController playerController;
    public TextMeshProUGUI killsText;

    private int oldKillsValue;

    private void Awake()
    {
        oldKillsValue = PlayerPrefs.GetInt("Total Kills", 0);
        killsText.text = "Total Kills: " + oldKillsValue;

        PlayerController.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
    }

    private void OnLocalPlayerSpawned(PlayerController pc)
    {
        playerController = pc;
        playerController.OnKillsChanged += OnSeshKillsChanged;
    }

    private void OnSeshKillsChanged(int newKillsValue)
    {
        int totalKillsValue = oldKillsValue + newKillsValue;

        PlayerPrefs.SetInt("Total Kills", totalKillsValue);
        PlayerPrefs.Save();

        killsText.text = "Total Kills: " + totalKillsValue;
    }

    private void OnDestroy()
    {
        PlayerController.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;

        if (playerController != null)
            playerController.OnKillsChanged -= OnSeshKillsChanged;
    }
}
