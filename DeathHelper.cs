using UnityEngine;
using Unity.Netcode;

public class DeathHelper : NetworkBehaviour
{
    public Canvas DeathCanvas;
    public Camera MenuCam;
    

    private PlayerController localPlayer;

    void Start()
    {
        PlayerController.OnLocalPlayerSpawned += (pc) => localPlayer = pc;

        // Make sure the menu camera starts hidden
        if (MenuCam != null)
            MenuCam.gameObject.SetActive(true);
        if (DeathCanvas != null)
            DeathCanvas.gameObject.SetActive(false);
    }

    void Update()
    {
        if (localPlayer == null)
        {
            localPlayer = FindLocalPlayer();
            MenuCam.gameObject.SetActive(true);
            MenuCam.enabled = true;
            DeathCanvas.gameObject.SetActive(false);

            return;
        }

        if (localPlayer.isDead.Value)
        {
            // Disable the player camera so MenuCam can take over cleanly
            if (localPlayer.TryGetComponent(out Camera pcCam))
                pcCam.enabled = false;

            // Activate the death camera and UI
            MenuCam.gameObject.SetActive(true);
            MenuCam.enabled = true;
            MenuCam.depth = 10; // ensure it renders on top
            MenuCam.tag = "MainCamera";
            MenuCam.backgroundColor = Color.black;

            DeathCanvas.gameObject.SetActive(true);
        }
        else if (localPlayer.camSwitched)
        {
            // Hide the death UI and make sure the menu camera is off
            DeathCanvas.gameObject.SetActive(false);
        }
    }

    private PlayerController FindLocalPlayer()
    {
        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            if (pc.IsOwner) return pc;
        return null;
    }
}
