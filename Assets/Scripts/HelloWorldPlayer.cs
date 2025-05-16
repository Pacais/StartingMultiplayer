using Unity.Netcode;
using UnityEngine;

namespace HelloWorld
{
    public class HelloWorldPlayer : NetworkBehaviour
    {
        public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();

        private Renderer rend;

        private Color[] availableColors = new Color[]
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.magenta
        };

        public NetworkVariable<int> colorIndex = new NetworkVariable<int>(
            -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            rend = GetComponent<Renderer>();

            if (IsOwner)
            {
                Move();
                RequestInitialColorServerRpc();
            }

            // Asegúrate de actualizar a cor local cando se modifique a variable sincronizada
            colorIndex.OnValueChanged += OnColorChanged;
        }

        private void OnColorChanged(int previous, int current)
        {
            if (current >= 0 && current < availableColors.Length)
            {
                rend.material.color = availableColors[current];
            }
        }

        public void Move()
        {
            SubmitPositionRequestRpc();
        }

        [Rpc(SendTo.Server)]
        private void SubmitPositionRequestRpc(RpcParams rpcParams = default)
        {
            var randomPosition = GetRandomPositionOnPlane();
            transform.position = randomPosition;
            Position.Value = randomPosition;
        }

        static Vector3 GetRandomPositionOnPlane()
        {
            return new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
        }

        [Rpc(SendTo.Server)]
        private void RequestInitialColorServerRpc(RpcParams rpcParams = default)
        {
            colorIndex.Value = Random.Range(0, availableColors.Length);
        }

        [Rpc(SendTo.Server)]
        public void RequestChangeColorServerRpc(RpcParams rpcParams = default)
        {
            colorIndex.Value = Random.Range(0, availableColors.Length);
        }

        public void ChangeColorRequest()
        {
            RequestChangeColorServerRpc();
        }

        private void Update()
        {
            transform.position = Position.Value;
        }
    }
}
