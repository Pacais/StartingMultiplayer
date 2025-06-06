using Unity.Netcode;
using UnityEngine;

namespace HelloWorld
{
    public class HelloWorldPlayer : NetworkBehaviour
    {
        public NetworkVariable<Color> PlayerColor = new NetworkVariable<Color>(
            writePerm: NetworkVariableWritePermission.Server
        );

        private float moveSpeed = 5f;

        private void Update()
        {
            // Só move o Owner, o NetworkTransform sincroniza aos demais
            if (!IsOwner) return;

            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            Vector3 movement = new Vector3(moveX, 0f, moveZ) * moveSpeed * Time.deltaTime;
            transform.position += movement;
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                OnColorChanged(Color.white, PlayerColor.Value);
            }

            PlayerColor.OnValueChanged += OnColorChanged;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestColorChangeServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var mgr = FindFirstObjectByType<HelloWorldManager>();
            mgr.AssignNewColor(clientId);
        }

        private void OnColorChanged(Color oldColor, Color newColor)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer == null) return;

            renderer.material = new Material(renderer.sharedMaterial);
            renderer.material.color = newColor;
        }
    }
}
