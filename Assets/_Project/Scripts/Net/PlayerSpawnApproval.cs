using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Schutzhuette.Net
{
    /// <summary>
    /// Prüft auf dem Host jede neue Verbindung (Connection Approval), bevor ein
    /// Spieler-Objekt entsteht. Vergibt einen freien Spawnpunkt und lehnt ab,
    /// wenn die Hütte voll ist oder der Spieler eine andere Version hat (<see cref="GameVersion"/>).
    /// </summary>
    /// <remarks>
    /// Warum Connection Approval: Die Antwort enthält Position und Rotation des
    /// Spielers. NGO schickt diese Startpose direkt mit der Spawn-Nachricht an
    /// alle, es braucht also keine eigene Synchronisierung. Und der Host
    /// entscheidet (Architektur-Regel 2), nicht der beitretende Client.
    /// Liegt auf demselben GameObject wie der NetworkManager. Im NetworkManager
    /// muss das Häkchen "Connection Approval" gesetzt sein, sonst ruft NGO
    /// diesen Callback nie auf.
    /// </remarks>
    [RequireComponent(typeof(NetworkManager))]
    public class PlayerSpawnApproval : MonoBehaviour
    {
        [Tooltip("Startpunkte der Spieler, in Vergabe-Reihenfolge.")]
        [SerializeField] private Transform[] _spawnPoints = new Transform[0];

        [Tooltip("Höchstzahl gleichzeitiger Spieler (Konzept: 1 bis 4).")]
        [SerializeField, Range(1, 4)] private int _maxPlayers = 4;

        private NetworkManager _networkManager;

        // Welcher Client steht auf welchem Spawnpunkt? Wird nur auf dem Host gefüllt.
        // Bewusst kein static: Mit ausgeschaltetem Domain Reload würde ein statisches
        // Feld seinen Inhalt ins nächste Play mitnehmen.
        private readonly Dictionary<ulong, int> _slotByClient = new Dictionary<ulong, int>();

        /// <summary>
        /// Anzahl der vergebbaren Plätze: das Kleinere aus Spawnpunkten und Höchstzahl.
        /// </summary>
        public int SlotCount => Mathf.Min(_spawnPoints.Length, _maxPlayers);

        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();

            if (_spawnPoints.Length < _maxPlayers)
            {
                Debug.LogWarning(
                    $"[Spawn] Nur {_spawnPoints.Length} Spawnpunkte für {_maxPlayers} Spieler. " +
                    $"Es können höchstens {SlotCount} Spieler beitreten.", this);
            }
        }

        private void OnEnable()
        {
            _networkManager.ConnectionApprovalCallback = ApproveConnection;
            _networkManager.OnClientDisconnectCallback += HandleClientDisconnect;
            _networkManager.OnServerStopped += HandleServerStopped;
        }

        private void OnDisable()
        {
            if (_networkManager == null)
            {
                return;
            }

            _networkManager.ConnectionApprovalCallback = null;
            _networkManager.OnClientDisconnectCallback -= HandleClientDisconnect;
            _networkManager.OnServerStopped -= HandleServerStopped;
        }

        // Läuft nur auf dem Host, einmal pro Verbindungsversuch.
        private void ApproveConnection(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            // Andere Version als der Host: ablehnen, mit Grund. Der eigene Client des Hosts
            // ist dasselbe Programm und wird nicht geprüft.
            string clientVersion = GameVersion.FromConnectionData(request.Payload);
            if (request.ClientNetworkId != NetworkManager.ServerClientId
                && !GameVersion.Matches(GameVersion.Current, clientVersion))
            {
                response.Approved = false;
                response.Reason = GameVersion.MismatchReason(GameVersion.Current, clientVersion);
                response.Pending = false;
                string shown = string.IsNullOrEmpty(clientVersion) ? "alt/unbekannt" : clientVersion;
                Debug.Log($"[Spawn] Client {request.ClientNetworkId} abgelehnt: Version {shown}, Host {GameVersion.Current}.");
                return;
            }

            int slot = FindFreeSlot();
            if (slot < 0)
            {
                // Abgelehnt: NGO schickt dem Client den Grund mit, der Client
                // liest ihn danach aus NetworkManager.DisconnectReason.
                response.Approved = false;
                response.Reason = $"Die Hütte ist voll ({SlotCount} Spieler).";
                response.Pending = false;
                Debug.Log($"[Spawn] Client {request.ClientNetworkId} abgelehnt: kein freier Platz.");
                return;
            }

            _slotByClient[request.ClientNetworkId] = slot;
            Transform spawnPoint = _spawnPoints[slot];

            response.Approved = true;
            response.CreatePlayerObject = true; // nimmt das Player Prefab aus dem NetworkManager
            response.Position = spawnPoint.position;
            response.Rotation = spawnPoint.rotation;
            response.Pending = false;

            Debug.Log($"[Spawn] Client {request.ClientNetworkId} angenommen, Spawnpunkt {slot + 1}.");
        }

        // Erster Spawnpunkt, auf dem gerade niemand steht, sonst -1.
        private int FindFreeSlot()
        {
            for (int slot = 0; slot < SlotCount; slot++)
            {
                if (_spawnPoints[slot] != null && !_slotByClient.ContainsValue(slot))
                {
                    return slot;
                }
            }

            return -1;
        }

        // Ein Spieler geht: Sein Spawnpunkt wird wieder frei.
        private void HandleClientDisconnect(ulong clientId)
        {
            _slotByClient.Remove(clientId);
        }

        // Host beendet: Alle Plätze frei für die nächste Runde im selben Play.
        private void HandleServerStopped(bool wasHost)
        {
            _slotByClient.Clear();
        }
    }
}
