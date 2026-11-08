using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace Schutzhuette.Net
{
    /// <summary>
    /// Selbst-Update über GitHub-Releases: prüft beim Start, ob es eine neuere Version gibt,
    /// und installiert sie auf Knopfdruck (laden, Prüfsumme kontrollieren, entpacken,
    /// Spiel beenden, Dateien tauschen, neu starten).
    /// </summary>
    /// <remarks>
    /// Das Menü (<see cref="ConnectionHud"/>) zeigt den Zustand und den Knopf. Nur im fertigen
    /// Spiel, nicht im Editor. Hat mit Netcode nichts zu tun: Jeder Spieler aktualisiert sein
    /// eigenes Spiel. Gleiche Versionen erzwingt die Versionsprüfung beim Beitreten.
    /// Zum Testen ohne neues Release: Start mit "-updateApiUrl &lt;API-Adresse eines Releases&gt;"
    /// und "-updateFakeVersion 0.0.1" (das Spiel hält sich dann für älter).
    /// </remarks>
    public class GameUpdater : MonoBehaviour
    {
        /// <summary>Wo das Update gerade steht.</summary>
        public enum UpdateState
        {
            /// <summary>Noch nicht geprüft oder im Editor.</summary>
            Idle,
            /// <summary>Fragt GitHub nach dem neuesten Release.</summary>
            Checking,
            /// <summary>Kein neueres Release.</summary>
            UpToDate,
            /// <summary>Konnte nicht prüfen (z. B. offline).</summary>
            CheckFailed,
            /// <summary>Neuere Version verfügbar, wartet auf den Knopf.</summary>
            Available,
            /// <summary>Lädt die Zip.</summary>
            Downloading,
            /// <summary>Prüft und entpackt.</summary>
            Installing,
            /// <summary>Spiel beendet sich, das Tausch-Skript übernimmt.</summary>
            Restarting,
            /// <summary>Etwas ging schief, Grund in <see cref="Message"/>.</summary>
            Failed,
        }

        [Tooltip("GitHub-API-Adresse des neuesten Releases.")]
        [SerializeField] private string _latestReleaseUrl = "https://api.github.com/repos/desirzzzz/Schutzhuette/releases/latest";

        [Tooltip("Name der Release-Datei beginnt hiermit und endet auf .zip.")]
        [SerializeField] private string _assetPrefix = "Schutzhuette-Prototyp-";

        [Tooltip("Höchstens so viele Sekunden für die Anfrage an GitHub.")]
        [SerializeField, Min(1)] private int _checkTimeoutSeconds = 10;

        private ReleaseInfo.Asset _asset;
        private UnityWebRequest _download;
        private string _localVersion;

        /// <summary>Aktueller Zustand.</summary>
        public UpdateState State { get; private set; } = UpdateState.Idle;

        /// <summary>Version des neuesten Releases (sobald geprüft).</summary>
        public string LatestVersion { get; private set; } = string.Empty;

        /// <summary>Fehlermeldung bei <see cref="UpdateState.Failed"/>.</summary>
        public string Message { get; private set; } = string.Empty;

        /// <summary>Ladefortschritt von 0 bis 1.</summary>
        public float Progress => _download != null ? _download.downloadProgress : 0f;

        private void Start()
        {
            // Im Editor nie: Dort gibt es nichts zu ersetzen, und jedes Play würde GitHub fragen.
            if (Application.isEditor)
            {
                return;
            }

            _localVersion = CommandLineValue("-updateFakeVersion") ?? GameVersion.Current;
            _latestReleaseUrl = CommandLineValue("-updateApiUrl") ?? _latestReleaseUrl;
            StartCoroutine(CheckForUpdate());
        }

        /// <summary>Installiert die verfügbare neuere Version. Nur bei <see cref="UpdateState.Available"/> oder nach einem Fehler.</summary>
        public void InstallUpdate()
        {
            if (_asset != null && (State == UpdateState.Available || State == UpdateState.Failed))
            {
                StartCoroutine(DownloadAndInstall());
            }
        }

        private IEnumerator CheckForUpdate()
        {
            State = UpdateState.Checking;
            using (UnityWebRequest request = UnityWebRequest.Get(_latestReleaseUrl))
            {
                request.timeout = _checkTimeoutSeconds;
                request.SetRequestHeader("Accept", "application/vnd.github+json");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    State = UpdateState.CheckFailed;
                    Debug.Log($"[Update] Prüfung nicht möglich: {request.error}");
                    yield break;
                }

                ReleaseInfo release = JsonUtility.FromJson<ReleaseInfo>(request.downloadHandler.text);
                LatestVersion = GameVersion.FromTag(release?.tag_name);
                _asset = FindZipAsset(release);

                if (_asset == null || !GameVersion.IsNewer(LatestVersion, _localVersion))
                {
                    State = UpdateState.UpToDate;
                    Debug.Log($"[Update] Aktuell: {_localVersion}, neuestes Release {LatestVersion}.");
                    yield break;
                }

                State = UpdateState.Available;
                Debug.Log($"[Update] Neue Version {LatestVersion} verfügbar (habe {_localVersion}).");
            }
        }

        private IEnumerator DownloadAndInstall()
        {
            Message = string.Empty;
            string gameFolder = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath));
            if (!UpdateInstaller.CanWriteTo(gameFolder))
            {
                Fail("Kein Schreibzugriff auf den Spielordner. Spiel z. B. auf den Desktop verschieben.");
                yield break;
            }

            // Arbeitsordner im Temp-Verzeichnis, jedes Mal frisch.
            string workFolder = Path.Combine(Path.GetTempPath(), "Schutzhuette-Update");
            try
            {
                if (Directory.Exists(workFolder))
                {
                    Directory.Delete(workFolder, true);
                }

                Directory.CreateDirectory(workFolder);
            }
            catch (Exception exception)
            {
                Fail("Temp-Ordner nicht nutzbar: " + exception.Message);
                yield break;
            }

            string zipPath = Path.Combine(workFolder, _asset.name);
            State = UpdateState.Downloading;
            Debug.Log($"[Update] Lade {_asset.browser_download_url}");

            _download = new UnityWebRequest(_asset.browser_download_url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerFile(zipPath) { removeFileOnAbort = true },
            };
            yield return _download.SendWebRequest();

            bool downloaded = _download.result == UnityWebRequest.Result.Success;
            string downloadError = _download.error;
            _download.Dispose();
            _download = null;
            if (!downloaded)
            {
                Fail("Download fehlgeschlagen: " + downloadError);
                yield break;
            }

            // Prüfen und entpacken im Hintergrund, damit das Fenster nicht einfriert.
            State = UpdateState.Installing;
            string expectedHash = ExpectedSha256(_asset);
            string extractFolder = Path.Combine(workFolder, "extract");
            Task<string> install = Task.Run(() => PrepareInstall(zipPath, expectedHash, _asset.size, extractFolder));
            while (!install.IsCompleted)
            {
                yield return null;
            }

            if (install.IsFaulted || install.Result == null)
            {
                Fail(install.Exception?.GetBaseException().Message ?? "Neue Version unvollständig.");
                yield break;
            }

            string exePath = Path.Combine(gameFolder, ExeName());
            string newBuild = UpdateInstaller.FindBuildFolder(install.Result, Path.GetFileName(exePath));
            if (newBuild == null)
            {
                Fail("In der neuen Version fehlt " + Path.GetFileName(exePath) + ".");
                yield break;
            }

            State = UpdateState.Restarting;
            Debug.Log($"[Update] Tausche {gameFolder} gegen {LatestVersion} und starte neu.");
            try
            {
                UpdateInstaller.LaunchSwap(workFolder, Process.GetCurrentProcess().Id, newBuild, gameFolder, exePath);
            }
            catch (Exception exception)
            {
                Fail("Update-Skript startet nicht: " + exception.Message);
                yield break;
            }

            Application.Quit();
        }

        // Hintergrund-Thread: Größe und Prüfsumme kontrollieren, dann entpacken.
        // Gibt den Entpack-Ordner zurück. Fehler kommen als Ausnahme zurück.
        private static string PrepareInstall(string zipPath, string expectedHash, long expectedSize, string extractFolder)
        {
            long size = new FileInfo(zipPath).Length;
            if (expectedSize > 0 && size != expectedSize)
            {
                throw new IOException($"Download unvollständig ({size} von {expectedSize} Bytes).");
            }

            if (!string.IsNullOrEmpty(expectedHash)
                && !string.Equals(UpdateInstaller.Sha256Of(zipPath), expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("Prüfsumme passt nicht, Download beschädigt.");
            }

            UpdateInstaller.ExtractSafely(zipPath, extractFolder);
            return extractFolder;
        }

        // Unity legt neben "Name.exe" den Ordner "Name_Data" an (Application.dataPath).
        private static string ExeName()
        {
            string dataFolder = Path.GetFileName(Application.dataPath.TrimEnd('/', '\\'));
            const string suffix = "_Data";
            string name = dataFolder.EndsWith(suffix) ? dataFolder.Substring(0, dataFolder.Length - suffix.Length) : dataFolder;
            return name + ".exe";
        }

        private void Fail(string message)
        {
            Message = message;
            State = UpdateState.Failed;
            Debug.LogWarning("[Update] " + message);
        }

        private ReleaseInfo.Asset FindZipAsset(ReleaseInfo release)
        {
            if (release?.assets == null)
            {
                return null;
            }

            foreach (ReleaseInfo.Asset asset in release.assets)
            {
                if (asset.name.StartsWith(_assetPrefix, StringComparison.OrdinalIgnoreCase)
                    && asset.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    return asset;
                }
            }

            return null;
        }

        // GitHub liefert "sha256:abc...", wir brauchen nur "abc...".
        private static string ExpectedSha256(ReleaseInfo.Asset asset)
        {
            const string prefix = "sha256:";
            return asset.digest != null && asset.digest.StartsWith(prefix) ? asset.digest.Substring(prefix.Length) : null;
        }

        private static string CommandLineValue(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == name)
                {
                    return arguments[i + 1];
                }
            }

            return null;
        }

        /// <summary>
        /// Antwort der GitHub-API für ein Release, nur die Felder, die wir brauchen.
        /// Die Feldnamen müssen genau wie im JSON heißen (JsonUtility), daher klein mit Unterstrich.
        /// </summary>
        [Serializable]
        private class ReleaseInfo
        {
            public string tag_name;
            public Asset[] assets;

            [Serializable]
            public class Asset
            {
                public string name;
                public long size;
                public string digest;
                public string browser_download_url;
            }
        }
    }
}
