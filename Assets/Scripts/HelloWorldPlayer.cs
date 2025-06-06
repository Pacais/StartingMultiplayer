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

        // Actualizar posición visual cando cambie Position
        Position.OnValueChanged += (oldPos, newPos) =>
        {
            transform.position = newPos;
        };

        // Actualizar cor cando cambie CurrentZone
        CurrentZone.OnValueChanged += (oldZone, newZone) =>
        {
            SetColorByZone(newZone);
        };

        // Inicializar posición e cor visual
        transform.position = Position.Value;
        SetColorByZone(CurrentZone.Value);
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Detectar teleport ao pulsar M
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (IsServer)
            {
                manager.TeleportAllToCenter();
            }
            else
            {
                Vector3 centerPos = manager.GetSpawnPosition();
                TeleportRequestServerRpc(centerPos);
            }
            return; // Saímos para non mover ao mesmo tempo
        }

        // Movemento normal
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        if (moveX == 0f && moveZ == 0f) return;

        Vector3 movement = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
        Vector3 newPos = transform.position + movement;

        int newZone = DetermineZone(newPos.x);

       if (newZone != CurrentZone.Value)
{
    // Pedir permiso ao servidor para cambiar de zona con nova posición
    manager.RequestMoveZoneServerRpc(newPos, newZone);
}

        else
        {
            // Movemento dentro da mesma zona
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
        Position.Value = newPosition;
    }

    [ServerRpc(RequireOwnership = true)]
    private void TeleportRequestServerRpc(Vector3 pos, ServerRpcParams rpcParams = default)
    {
        Position.Value = pos;
        CurrentZone.Value = 0;
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
