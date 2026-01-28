using UnityEngine;
using Unity.Netcode;
using System;
using Debug = UnityEngine.Debug;

public class PlayerController : NetworkBehaviour
{
    public static event Action<PlayerController> OnLocalPlayerSpawned;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener playerAudioListener;

    public float xDirection;
    public float yDirection;
    public float zDirection;
    public float jumpForce = 7.5f;
    public float rayLength = 3f;
    public float gunRange = 25f;

    // vec3 is 3d coords
    public Vector3 walkingDirection;
    public Vector3 movementDirection;
    public Vector3 hitPos;

    public int speed = 10;
    public int rotationSpeed = 360;

    public bool onFloor;
    public bool jump;
    public bool broTouch;

    public bool camSwitched = false;
    public bool suicide;
    public NetworkVariable<bool> isDead;

    public Rigidbody rb;

    // stateexmachina
    public enum States
    {
        idle,
        walking,
        jumping,
        dead
    }

    public States state;

    public override void OnNetworkSpawn()
    {
        rb = gameObject.GetComponent<Rigidbody>();

        if (IsOwner)
            OnLocalPlayerSpawned?.Invoke(this);

        state = States.idle;
        
        Debug.Log($"{name} write perm = {isDead.WritePerm}");
    }

    void Awake()
    {
        isDead = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        var method = typeof(NetworkBehaviour).GetMethod(
            "__initializeNetworkVariable",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        method?.Invoke(this, new object[] { isDead });
    }


    // Update is called once per frame
    void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        CheckInputs();

        if (IsLocalPlayer)
        {
            playerCamera.enabled = true;
            playerAudioListener.enabled = true;
            camSwitched = true;
        }
        else
        {
            playerCamera.enabled = false;
            playerAudioListener.enabled = false;
            camSwitched = false;
        }

        // walkDir of player
        walkingDirection = zDirection * transform.forward;
        walkingDirection.Normalize();

        if (suicide)
        {
            isDead.Value = true;
        }

        if (isDead.Value == true && Input.GetKeyUp(KeyCode.Escape))
        {
            Disconnect();
        }

        // rotDir of player
        movementDirection = (zDirection * transform.forward) + (xDirection * transform.right);
        movementDirection.Normalize();

        // rotation movement
        if ((movementDirection != Vector3.zero) && (xDirection != 0))
        {
            Quaternion toRotation = Quaternion.LookRotation(movementDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        }

        // yumps
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jump = true;
        }

        // shoot input
        if (Input.GetMouseButtonDown(0))
        {
            ShotFired();
        }

        // states
        if (state == States.idle)
        {
            IdleState();
        }
        else if (state == States.walking)
        {
            WalkingState();
        }
        else if (state == States.jumping)
        {
            JumpingState();
        }
        else if (state == States.dead)
        {
            DeadState();
        }
    }

    private void FixedUpdate()
    {
        CheckGroundStatus();

        rb.AddForce(walkingDirection * speed, ForceMode.Force);

        if (jump && onFloor)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        jump = false;
    }

    public void IdleState()
    {
        if (((xDirection != 0) || (zDirection != 0)) && !jump)
        {
            state = States.walking;
        }
        else if (jump || !onFloor)
        {
            state = States.jumping;
        }
        else if (isDead.Value)
        {
            state = States.dead;
        }
    }

    public void WalkingState()
    {
        if ((zDirection == 0) && !jump)
        {
            state = States.idle;
        }
        else if (jump || !onFloor)
        {
            state = States.jumping;
        }
        else if (isDead.Value)
        {
            state = States.dead;
        }
    }

    public void JumpingState()
    {
        if (((xDirection != 0) || (zDirection != 0)) && onFloor)
        {
            state = States.walking;
        }
        else if ((zDirection == 0) && onFloor)
        {
            state = States.idle;
        }
        else if (isDead.Value)
        {
            state = States.dead;
        }
    }

    public void DeadState()
    {
        playerCamera.enabled = false;
        playerAudioListener.enabled = false;
    }

    public void CheckInputs()
    {
        xDirection = Input.GetAxis("Horizontal");
        zDirection = Input.GetAxis("Vertical");

        suicide = Input.GetKeyDown(KeyCode.K);
    }

    void Disconnect()
    {
        camSwitched = false;
        NetworkManager.Singleton.Shutdown();
        if (NetworkManager.Singleton.IsHost)
        {
            // TODO: betterer shutdown so no dead sessions
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Collided with other player!");
            broTouch = true;
            if (collision.rigidbody != null)
            {
                collision.rigidbody.AddExplosionForce(250f, transform.position, 32);
            }
        }
    }

    public void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Stopped colliding with other Player!");
            broTouch = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("KillBox"))
        {
            isDead.Value = true;
            Debug.Log("Player hit killbox");
        }
    }

    void CheckGroundStatus()
    {
        onFloor = Physics.Raycast(transform.position, Vector3.down, rayLength);
        Debug.DrawRay(transform.position, Vector3.down * rayLength, Color.red);
    }

    void ShotFired()
    {
        RaycastHit playerHit;
        Ray camRay = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // draw for 1 second so you can see it clearly
        Debug.DrawRay(camRay.origin, camRay.direction * gunRange, Color.green, 1f);

        if (Physics.Raycast(camRay, out playerHit, gunRange))
        {
            if (playerHit.collider != null && playerHit.collider.CompareTag("Player"))
            {
                NetworkObject targetNetObj = playerHit.collider.GetComponent<NetworkObject>();
                if (targetNetObj != null)
                {
                    hitPos = playerHit.point;
                    KillOtherServerRpc(targetNetObj, hitPos);
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void KillOtherServerRpc(NetworkObjectReference target, Vector3 hitPoint)
    {
        Debug.Log($"KillOtherServerRpc called on {(IsServer ? "SERVER" : "CLIENT")} by {OwnerClientId}");

        if (!IsServer) return; // safety

        if (target.TryGet(out NetworkObject targetObj))
        {
            // Apply a physics impulse if the target has a Rigidbody
            var targetRb = targetObj.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                targetRb.freezeRotation = false;
                targetRb.AddForceAtPosition(transform.forward * 25f, hitPoint, ForceMode.Impulse);
            }

            var pc = targetObj.GetComponent<PlayerController>();
            if (pc != null)
            {
                ulong originalOwner = targetObj.OwnerClientId;

                // If the server doesn't currently own this object, take ownership
                if (!targetObj.IsOwnedByServer)
                {
                    targetObj.ChangeOwnership(NetworkManager.ServerClientId);
                    Debug.Log($"Server temporarily took ownership of {targetObj.name}");
                }

                // Modify the NetworkVariable now that the server owns it
                pc.isDead.Value = true;
                Debug.Log($"Server set {targetObj.name}.isDead = true");

                // Optionally, give ownership back to the original client
                if (originalOwner != NetworkManager.ServerClientId)
                {
                    targetObj.ChangeOwnership(originalOwner);
                    Debug.Log($"Ownership returned to client {originalOwner}");
                }
            }
        }
    }

}
