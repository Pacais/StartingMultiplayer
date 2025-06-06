using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace HelloWorld
{
    public class HelloWorldManager : MonoBehaviour
    {
        private NetworkManager m_NetworkManager;

        private static readonly List<Color> MasterColors = new List<Color>
        {
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow,
            Color.magenta,
            Color.cyan
        };

        private readonly List<Color> _colorPool = new List<Color>();
        private readonly Dictionary<ulong, Color> _assignedColors = new Dictionary<ulong, Color>();

        private void Awake()
        {
            m_NetworkManager = GetComponent<NetworkManager>();
            _colorPool.AddRange(MasterColors);

            m_NetworkManager.NetworkConfig.ConnectionApproval = true;
            m_NetworkManager.ConnectionApprovalCallback += ApproveOrReject;
            m_NetworkManager.OnClientConnectedCallback += OnClientConnected;
            m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));

            if (!m_NetworkManager.IsClient && !m_NetworkManager.IsServer)
            {
                if (GUILayout.Button("Host")) m_NetworkManager.StartHost();
                if (GUILayout.Button("Client")) m_NetworkManager.StartClient();
                if (GUILayout.Button("Server")) m_NetworkManager.StartServer();
            }
            else
            {
                var mode = m_NetworkManager.IsHost ? "Host"
                         : m_NetworkManager.IsServer ? "Server"
                                                     : "Client";
                GUILayout.Label("Transporte: " +
                    m_NetworkManager.NetworkConfig.NetworkTransport.GetType().Name);
                GUILayout.Label("Modo: " + mode);

                // Quitamos botón de Move porque el movimiento ahora es manual por input

                // Botón para que el cliente pida cambio de color
                if (m_NetworkManager.IsClient)
                {
                    if (GUILayout.Button("Change Color"))
                    {
                        var local = m_NetworkManager.SpawnManager
                            .GetLocalPlayerObject()
                            .GetComponent<HelloWorldPlayer>();
                        local.RequestColorChangeServerRpc();
                    }
                }
            }

            GUILayout.EndArea();
        }

        private void ApproveOrReject(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse res)
        {
            if (_assignedColors.Count >= MasterColors.Count)
            {
                res.Approved = false;
                res.Reason = "Lobby lleno (máx. 6 jugadores).";
                return;
            }

            res.Approved = true;
            res.CreatePlayerObject = true;
            res.PlayerPrefabHash = null;
            res.Position = Vector3.zero;
            res.Rotation = Quaternion.identity;
        }

        private void OnClientConnected(ulong clientId)
        {
            AssignNewColor(clientId);
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (_assignedColors.TryGetValue(clientId, out var color))
            {
                _assignedColors.Remove(clientId);
                _colorPool.Add(color);
            }
        }

        public void AssignNewColor(ulong clientId)
        {
            if (_assignedColors.TryGetValue(clientId, out var old))
            {
                _colorPool.Add(old);
            }

            if (_colorPool.Count == 0)
            {
                Debug.LogWarning("¡No hay colores libres para asignar!");
                return;
            }

            Color newColor = _colorPool[0];
            _colorPool.RemoveAt(0);

            _assignedColors[clientId] = newColor;

            var player = m_NetworkManager.SpawnManager
                .GetPlayerNetworkObject(clientId)
                .GetComponent<HelloWorldPlayer>();
            player.PlayerColor.Value = newColor;
        }
    }
}
