using System.Text;
using UnityEngine;

namespace Schutzhuette.Net
{
    /// <summary>
    /// Versionsnummer des Spiels und der Vergleich beim Beitreten: Host und Gäste müssen
    /// genau dieselbe Version spielen.
    /// </summary>
    /// <remarks>
    /// Warum: Zwei verschiedene Versionen können sich zwar verbinden, haben aber zum Beispiel
    /// andere Netzwerk-Komponenten an den Kisten. Das führt zu schwer verständlichen Fehlern.
    /// Der Client schickt seine Version als Verbindungsdaten (NetworkConfig.ConnectionData)
    /// mit, der Host prüft sie in der Connection Approval (siehe PlayerSpawnApproval).
    /// Die Version kommt aus den Projekteinstellungen (Application.version, gesetzt von
    /// PrototypeBuildSetup). Reine Funktionen ohne Zustand.
    /// </remarks>
    public static class GameVersion
    {
        // Mehr nimmt der Host nicht an, damit niemand riesige Verbindungsdaten schicken kann.
        private const int MaxVersionBytes = 64;

        /// <summary>Version dieses Spiels, z. B. "0.1.2".</summary>
        public static string Current => Application.version;

        /// <summary>Version als Verbindungsdaten für den Beitritt.</summary>
        public static byte[] ToConnectionData(string version)
        {
            return Encoding.UTF8.GetBytes(version ?? string.Empty);
        }

        /// <summary>
        /// Version aus den Verbindungsdaten eines Clients. Leer, wenn er keine schickt
        /// (Versionen bis 0.1.1 kennen das noch nicht) oder die Daten unbrauchbar sind.
        /// </summary>
        public static string FromConnectionData(byte[] payload)
        {
            if (payload == null || payload.Length == 0 || payload.Length > MaxVersionBytes)
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(payload).Trim();
        }

        /// <summary>Passen die beiden Versionen zusammen? Nur bei genau gleicher Nummer.</summary>
        public static bool Matches(string hostVersion, string clientVersion)
        {
            return !string.IsNullOrEmpty(clientVersion) && clientVersion == hostVersion;
        }

        /// <summary>Meldung für den abgelehnten Spieler.</summary>
        public static string MismatchReason(string hostVersion, string clientVersion)
        {
            string yours = string.IsNullOrEmpty(clientVersion) ? "eine ältere" : clientVersion;
            return $"Falsche Version: Der Host spielt {hostVersion}, du {yours}. Bitte die Version {hostVersion} laden.";
        }
    }
}
