using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Schutzhuette.Net
{
    /// <summary>
    /// Provisorische Verbindungs-Knöpfe für den Prototyp: Host starten,
    /// beitreten, trennen, dazu eine Statuszeile.
    /// </summary>
    /// <remarks>
    /// Bewusst mit OnGUI (IMGUI) gebaut: Das braucht keinen Canvas und kein
    /// EventSystem in der Szene. Ein echtes Menü ersetzt es später.
    /// Liegt auf demselben GameObject wie NetworkManager und UnityTransport.
    /// </remarks>
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public class ConnectionHud : MonoBehaviour
    {
        [Tooltip("Adresse, mit der sich ein Client verbindet. 127.0.0.1 = derselbe PC.")]
        [SerializeField] private string _joinAddress = "127.0.0.1";

        [Tooltip("Port, auf dem der Host lauscht und mit dem sich Clients verbinden.")]
        [SerializeField] private ushort _port = 7777;

        [Tooltip("Vergrößert die Knöpfe, damit sie im Game-Fenster lesbar sind.")]
        [SerializeField, Range(1f, 3f)] private float _uiScale = 1.5f;

        private NetworkManager _networkManager;
        private UnityTransport _transport;
        private string _statusMessage = string.Empty;
        private float _smoothedFps;

        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();
            _transport = GetComponent<UnityTransport>();
        }

        private void OnEnable()
        {
            _networkManager.OnClientConnectedCallback += HandleClientConnected;
            _networkManager.OnClientDisconnectCallback += HandleClientDisconnect;
        }

        private void OnDisable()
        {
            if (_networkManager == null)
            {
                return;
            }

            _networkManager.OnClientConnectedCallback -= HandleClientConnected;
            _networkManager.OnClientDisconnectCallback -= HandleClientDisconnect;
        }

        // Bilder pro Sekunde, geglättet. Zeigt, ob ein Fenster (z. B. "Player 2") selbst ruckelt.
        private void Update()
        {
            float currentFps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            _smoothedFps = _smoothedFps <= 0f ? currentFps : Mathf.Lerp(_smoothedFps, currentFps, 0.05f);
        }

        private void OnGUI()
        {
            GUI.matrix = Matrix4x4.Scale(new Vector3(_uiScale, _uiScale, 1f));
            GUILayout.BeginArea(new Rect(10f, 10f, 260f, 400f));
            GUILayout.Label($"FPS: {_smoothedFps:0}");

            // IsListening: Läuft gerade eine Sitzung (als Host oder Client)?
            if (_networkManager.IsListening)
            {
                DrawSessionInfo();
            }
            else
            {
                DrawConnectButtons();
            }

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                GUILayout.Label(_statusMessage);
            }

            GUILayout.EndArea();
        }

        private void DrawConnectButtons()
        {
            if (GUILayout.Button("Host starten"))
            {
                StartHost();
            }

            GUILayout.Label("Adresse des Hosts:");
            _joinAddress = GUILayout.TextField(_joinAddress);

            if (GUILayout.Button("Beitreten"))
            {
                StartClient();
            }

            // Im Editor beendet das nichts, im fertigen Spiel schließt es das Fenster.
            if (GUILayout.Button("Beenden"))
            {
                Application.Quit();
            }
        }

        private void DrawSessionInfo()
        {
            string role = _networkManager.IsHost ? "Host" : _networkManager.IsServer ? "Server" : "Client";
            GUILayout.Label($"Rolle: {role}");
            GUILayout.Label($"Deine Client-ID: {_networkManager.LocalClientId}");

            // Die Liste aller Verbundenen kennt nur der Host.
            if (_networkManager.IsServer)
            {
                GUILayout.Label($"Spieler verbunden: {_networkManager.ConnectedClientsIds.Count}");
            }

            GUILayout.Label(BuildPingText());

            if (GUILayout.Button("Trennen"))
            {
                _networkManager.Shutdown();
                _statusMessage = "Getrennt.";
            }
        }

        // Gemessene Rundlaufzeit (Ping) laut Unity Transport. Zeigt, ob die simulierte
        // Verzögerung des Network Simulators wirklich ankommt.
        private string BuildPingText()
        {
            if (!_networkManager.IsServer)
            {
                return $"Ping zum Host: {_transport.GetCurrentRtt(NetworkManager.ServerClientId)} ms";
            }

            var pings = new List<string>();
            foreach (ulong clientId in _networkManager.ConnectedClientsIds)
            {
                // Der eigene Client des Hosts läuft ohne Netzwerk, sein Ping ist bedeutungslos.
                if (clientId != NetworkManager.ServerClientId)
                {
                    pings.Add($"{_transport.GetCurrentRtt(clientId)} ms");
                }
            }

            return pings.Count == 0 ? "Ping: (noch kein anderer Spieler)" : "Ping: " + string.Join(", ", pings);
        }

        private void StartHost()
        {
            // Der Host lauscht auf allen Netzwerkkarten (0.0.0.0). So können sich
            // später auch Freunde im LAN verbinden, nicht nur dieser PC.
            _transport.SetConnectionData("127.0.0.1", _port, "0.0.0.0");
            _statusMessage = _networkManager.StartHost()
                ? string.Empty
                : "Host konnte nicht starten (Port belegt?).";
        }

        private void StartClient()
        {
            _transport.SetConnectionData(_joinAddress, _port);
            _statusMessage = _networkManager.StartClient()
                ? "Verbinde ..."
                : "Beitreten konnte nicht starten.";
        }

        // Wird auf dem Client aufgerufen, sobald er selbst verbunden ist.
        private void HandleClientConnected(ulong clientId)
        {
            if (clientId == _networkManager.LocalClientId)
            {
                _statusMessage = string.Empty;
            }
        }

        // Auf dem Host meldet das andere Spieler, die gehen. Die Statuszeile
        // interessiert hier nur der Fall, dass WIR als Client getrennt wurden,
        // zum Beispiel weil die Hütte voll ist.
        private void HandleClientDisconnect(ulong clientId)
        {
            if (_networkManager.IsServer)
            {
                return;
            }

            string reason = _networkManager.DisconnectReason;
            _statusMessage = string.IsNullOrEmpty(reason)
                ? "Verbindung getrennt."
                : $"Getrennt: {reason}";
        }
    }
}
