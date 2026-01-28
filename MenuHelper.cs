using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using System.Collections;

public class MenuHelper : NetworkBehaviour
{
    public Color menuColor = new Color(50f / 255f, 130f / 255f, 50f / 255f, 1f);
    public Button CreateSesh;
    public Button JoinSesh;
    public Camera MenuCamera;
    public Canvas MenuCanvas;
    public Image Loader;
    public TextMeshProUGUI LoaderText;

    private PlayerController localPlayer;

    public bool CanDeactivate = false;
    public bool trying = false;
    void Awake()
    {
        CreateSesh.onClick.AddListener(HostClicked);
        JoinSesh.onClick.AddListener(ClientClicked);
    }

    IEnumerator WaitForCamSwitch()
    {
        // Wait until the local player actually exists
        yield return new WaitUntil(() => localPlayer != null);

        // Then wait until the player's camera has switched
        yield return new WaitUntil(() => localPlayer.camSwitched);

        CanDeactivate = true;
        trying = false;
    }

    void Update()
    {
        if (CanDeactivate)
        {
            MenuCamera.gameObject.SetActive(false);
            MenuCamera.enabled = false;
            MenuCanvas.gameObject.SetActive(false);
            LoaderText.enabled = false;
            Loader.enabled = false;
        }

        if (trying)
        {
            Loader.enabled = true;
            LoaderText.enabled = true;
            Loader.transform.Rotate(0, 0, -250f * Time.deltaTime);
        }

        if (localPlayer == null)
        {
            ReactivateMenu();
            localPlayer = FindlocalPlayer();
            return;
        }
    }

    void HostClicked()
    {
        StartCoroutine(WaitForCamSwitch());
        trying = true;
    }

    void ClientClicked()
    {
        StartCoroutine(WaitForCamSwitch());
        trying = true;
    }

    public void ReactivateMenu()
    {
        MenuCamera.backgroundColor = menuColor;
        MenuCamera.gameObject.SetActive(true);
        MenuCamera.enabled = true;
        MenuCanvas.gameObject.SetActive(true);
        CanDeactivate = false;
        CreateSesh.interactable = true;
        JoinSesh.interactable = true;
    }

    private PlayerController FindlocalPlayer()
    {

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (var pc in players)
        {
            if (pc.IsOwner)
                return pc;
        }
        return null;
    }
}
