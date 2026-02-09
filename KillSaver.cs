using UnityEngine;
using TMPro;

public class KillSaver : MonoBehaviour
{
    private PlayerController playerController;
    public TextMeshProUGUI killsText;

    private int oldKillsValue;
    private int totalKillsValue;

    private void Awake()
    {
        if (playerController != null)
        {
            playerController.OnKillsChanged += OnSeshKillsChanged;
            oldKillsValue = PlayerPrefs.GetInt("Total Kills");
        }
    }

    private void OnSeshKillsChanged(int newKillsValue)
    {
        totalKillsValue = oldKillsValue + newKillsValue;
        PlayerPrefs.SetInt("Total Kills", totalKillsValue);
        PlayerPrefs.Save();
        killsText.text = "Total Kills: " + totalKillsValue.ToString();
    }

    private void OnDestroy()
    {
        if (playerController != null)
        {
            playerController.OnKillsChanged -= OnSeshKillsChanged;
        }
    }
}
