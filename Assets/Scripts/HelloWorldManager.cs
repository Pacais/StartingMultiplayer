using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HelloWorldManager : NetworkBehaviour
{
    public GameObject plane;
    public float planeWidth = 10f;
    public int maxPlayersPerTeam = 2;

    private readonly List<ulong> team1Players = new List<ulong>();
    private readonly List<ulong> team2Players = new List<ulong>();
    private readonly List<ulong> noTeamPlayers = new List<ulong>();

    private float zoneBoundary = 1.5f;

    /// <summary>
    /// Devolve un punto aleatorio na franxa central (zona 0).
    /// </summary>
    public Vector3 GetRandomPositionInCenter()
    {
        float x = Random.Range(-zoneBoundary, zoneBoundary);
        float z = Random.Range(-planeWidth / 2f, planeWidth / 2f);
        return new Vector3(x, 1f, z);
    }

    /// <summary>
    /// Usado ao spawnear un xogador: cadrará na zona central.
    /// </summary>
    public Vector3 GetSpawnPosition()
    {
        return GetRandomPositionInCenter();
    }

    /// <summary>
    /// Lóxica para comprobar se un cliente pode entrar na zona 'zone'.
    /// - 0 = Sen equipo (zona central, sen límite).
    /// - 1 = Equipo 1 (ata maxPlayersPerTeam).
    /// - 2 = Equipo 2 (ata maxPlayersPerTeam).
    /// Devolve true se se move, false se o equipo está cheo.
    /// </summary>
    public bool TryMoveToZone(ulong clientId, int zone)
    {
        if (zone == 1)
        {
            if (team1Players.Count >= maxPlayersPerTeam && !team1Players.Contains(clientId))
                return false;
            RemovePlayerFromTeams(clientId);
            team1Players.Add(clientId);
            return true;
        }
        else if (zone == 2)
        {
            if (team2Players.Count >= maxPlayersPerTeam && !team2Players.Contains(clientId))
                return false;
            RemovePlayerFromTeams(clientId);
            team2Players.Add(clientId);
            return true;
        }
        else if (zone == 0)
        {
            RemovePlayerFromTeams(clientId);
            noTeamPlayers.Add(clientId);
            return true;
        }
        return false;
    }

    private void RemovePlayerFromTeams(ulong clientId)
    {
        team1Players.Remove(clientId);
        team2Players.Remove(clientId);
        noTeamPlayers.Remove(clientId);
    }

    /// <summary>
    /// ServerRpc chamado polos clientes para pedir permiso de cambio de zona,
    /// pasando tamén a posición á que o xogador intentou moverse.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveZoneServerRpc(Vector3 requestedPosition, int zone, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        bool canMove = TryMoveToZone(clientId, zone);

        if (!canMove)
        {
            // Se o equipo está cheo, non actualizamos Position nin CurrentZone.
            // Podes engadir un ClientRpc de aviso se queres feedback visual.
            return;
        }

        // Se está permitido, actualizamos a zona e a posición directamente:
        var networkObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
        if (networkObj == null) return;
        var player = networkObj.GetComponent<HelloWorldPlayer>();
        if (player == null) return;

        // 1) Actualizar zona
        player.CurrentZone.Value = zone;

        // 2) Actualizar posición ao valor que o cliente pasou
        player.Position.Value = requestedPosition;
    }
}
