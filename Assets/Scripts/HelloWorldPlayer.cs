using Unity.Netcode;
using UnityEngine;

public class HelloWorldPlayer : NetworkBehaviour
{
    public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> CurrentZone = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private HelloWorldManager manager;
    private float moveSpeed = 5f;

    public override void OnNetworkSpawn()
    {
        manager = FindObjectOfType<HelloWorldManager>();

        if (IsServer)
        {
            // Spawn inicial na zona central (0)
            Vector3 startPos = manager.GetSpawnPosition();
            Position.Value = startPos;
            CurrentZone.Value = 0;
        }

        // Cando Position cambie no servidor, actualizamos transform:
        Position.OnValueChanged += (oldPos, newPos) =>
        {
            transform.position = newPos;
        };

        // Cando CurrentZone cambie, actualizamos cor:
        CurrentZone.OnValueChanged += (oldZone, newZone) =>
        {
            SetColorByZone(newZone);
        };

        // Inicializamos posición e cor no cliente
        transform.position = Position.Value;
        SetColorByZone(CurrentZone.Value);
    }

    private void Update()
    {
        if (!IsOwner) return;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        if (moveX == 0f && moveZ == 0f) return;

        Vector3 movement = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
        Vector3 newPos = transform.position + movement;

        int newZone = DetermineZone(newPos.x);

        if (newZone != CurrentZone.Value)
        {
            // Pedir ao servidor permiso para cambiar de zona, enviando a posición desexada
            manager.RequestMoveZoneServerRpc(newPos, newZone);
        }
        else
        {
            // Movemento dentro da mesma zona: pedimos ao servidor que actualice Position.
            RequestMoveServerRpc(newPos);
        }
    }

    private int DetermineZone(float xPos)
    {
        float boundary = 1.5f;
        if (xPos < -boundary) return 1; // equipo 1
        if (xPos > boundary) return 2;  // equipo 2
        return 0;                       // zona central
    }

    [ServerRpc(RequireOwnership = true)]
    private void RequestMoveServerRpc(Vector3 newPosition, ServerRpcParams rpcParams = default)
    {
        // Só o servidor pode escribir Position
        Position.Value = newPosition;
    }

    public void SetColorByZone(int zone)
    {
        Color color = zone switch
        {
            1 => Color.blue,
            2 => Color.red,
            _ => Color.white
        };

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = color;
    }
}
