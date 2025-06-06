using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    public float speed = 5f;
    public NetworkVariable<Vector3> NetworkedPosition = new NetworkVariable<Vector3>();

    private CharacterController controller;

    public override void OnNetworkSpawn()
    {
        controller = GetComponent<CharacterController>();

        // Se non é dono, só lerá a posición da NetworkVariable
        if (!IsOwner)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        // Só o dono move con input
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        Vector3 move = input * speed * Time.deltaTime;

        controller.Move(move);

        // Actualiza a posición sincronizada no servidor
        if (IsOwner)
        {
            NetworkedPosition.Value = transform.position;
        }
    }

    private void LateUpdate()
    {
        // Para todos (non donos): seguir a posición sincronizada
        if (!IsOwner)
        {
            transform.position = NetworkedPosition.Value;
        }
    }
}
