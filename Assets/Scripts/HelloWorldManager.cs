using Unity.Netcode;
using UnityEngine;

namespace HelloWorld
{
 
    public class HelloWorldManager : MonoBehaviour
    {
        private NetworkManager m_NetworkManager;

        private void Awake()
        {
            m_NetworkManager = GetComponent<NetworkManager>();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));

            if (!m_NetworkManager.IsClient && !m_NetworkManager.IsServer)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();
                ActionButtons();
            }

            GUILayout.EndArea();
        }

        private void StartButtons()
        {
            if (GUILayout.Button("Host")) m_NetworkManager.StartHost();
            if (GUILayout.Button("Client")) m_NetworkManager.StartClient();
            if (GUILayout.Button("Server")) m_NetworkManager.StartServer();
        }

        private void StatusLabels()
        {
            var mode = m_NetworkManager.IsHost ? "Host" :
                       m_NetworkManager.IsServer ? "Server" : "Client";

            GUILayout.Label("Transport: " +
                m_NetworkManager.NetworkConfig.NetworkTransport.GetType().Name);
            GUILayout.Label("Mode: " + mode);
        }

        private void ActionButtons()
        {
            // Botón para mover o xogador
            if (GUILayout.Button(m_NetworkManager.IsServer ? "Move" : "Request Position Change"))
            {
                if (m_NetworkManager.IsServer && !m_NetworkManager.IsClient)
                {
                    foreach (ulong uid in m_NetworkManager.ConnectedClientsIds)
                    {
                        var playerObj = m_NetworkManager.SpawnManager.GetPlayerNetworkObject(uid);
                        playerObj.GetComponent<HelloWorldPlayer>().Move();
                    }
                }
                else
                {
                    var playerObj = m_NetworkManager.SpawnManager.GetLocalPlayerObject();
                    playerObj.GetComponent<HelloWorldPlayer>().Move();
                }
            }

            // Botón para cambiar de cor
            if (GUILayout.Button("Change Color"))
            {
                var playerObj = m_NetworkManager.SpawnManager.GetLocalPlayerObject();
                playerObj.GetComponent<HelloWorldPlayer>().ChangeColorRequest();
            }
        }
    }
}
