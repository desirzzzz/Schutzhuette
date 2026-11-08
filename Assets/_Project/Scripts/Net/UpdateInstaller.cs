using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Schutzhuette.Net
{
    /// <summary>
    /// Dateiarbeit für das Selbst-Update: Prüfsumme kontrollieren, Zip sicher entpacken und
    /// ein kleines PowerShell-Skript starten, das die Dateien tauscht, sobald das Spiel zu ist.
    /// </summary>
    /// <remarks>
    /// Warum ein Skript: Eine laufende Exe und ihre Dateien sind unter Windows gesperrt. Das Spiel
    /// kann sich also nicht selbst überschreiben. Es beendet sich, das Skript wartet darauf,
    /// kopiert die neue Version mit robocopy über die alte und startet das Spiel neu.
    /// Reine Funktionen ohne Zustand. Läuft teils in einem Hintergrund-Thread, deshalb hier
    /// keine Unity-Aufrufe.
    /// </remarks>
    public static class UpdateInstaller
    {
        /// <summary>SHA-256 einer Datei als Kleinbuchstaben-Hex.</summary>
        public static string Sha256Of(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(stream);
                var text = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    text.Append(b.ToString("x2"));
                }

                return text.ToString();
            }
        }

        /// <summary>
        /// Entpackt eine Zip in einen leeren Ordner. Bricht ab, wenn ein Eintrag aus dem Ordner
        /// hinaus zeigen würde (z. B. "../../Windows/..."), damit eine manipulierte Zip nichts
        /// anderes überschreiben kann.
        /// </summary>
        public static void ExtractSafely(string zipPath, string targetFolder)
        {
            string root = Path.GetFullPath(targetFolder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(root);

            using (FileStream stream = File.OpenRead(zipPath))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string destination = Path.GetFullPath(Path.Combine(root, entry.FullName));
                    if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new IOException($"Ungültiger Pfad in der Zip: {entry.FullName}");
                    }

                    // Ordner-Einträge enden auf "/" und haben keinen Inhalt.
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destination);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    using (Stream input = entry.Open())
                    using (FileStream output = File.Create(destination))
                    {
                        input.CopyTo(output);
                    }
                }
            }
        }

        /// <summary>
        /// Sucht im entpackten Ordner den Spielordner: dort, wo die Exe mit diesem Namen liegt
        /// (direkt oder einen Ordner tiefer, die Release-Zip hat einen Hauptordner).
        /// </summary>
        /// <returns>Pfad des Spielordners oder null.</returns>
        public static string FindBuildFolder(string extractedFolder, string exeName)
        {
            if (File.Exists(Path.Combine(extractedFolder, exeName)))
            {
                return extractedFolder;
            }

            foreach (string folder in Directory.GetDirectories(extractedFolder))
            {
                if (File.Exists(Path.Combine(folder, exeName)))
                {
                    return folder;
                }
            }

            return null;
        }

        /// <summary>Darf in diesen Ordner geschrieben werden? (Nicht in "Programme" ohne Adminrechte.)</summary>
        public static bool CanWriteTo(string folder)
        {
            try
            {
                string probe = Path.Combine(folder, ".update-probe");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Schreibt das Tausch-Skript und startet es. Danach muss sich das Spiel sofort beenden.
        /// </summary>
        /// <param name="workFolder">Arbeitsordner des Updates (für das Skript).</param>
        /// <param name="gameProcessId">Prozess-ID des laufenden Spiels, auf dessen Ende gewartet wird.</param>
        /// <param name="newBuildFolder">Entpackte neue Version.</param>
        /// <param name="gameFolder">Ordner des laufenden Spiels, wird überschrieben.</param>
        /// <param name="exePath">Exe, die danach neu gestartet wird.</param>
        public static void LaunchSwap(string workFolder, int gameProcessId, string newBuildFolder, string gameFolder, string exePath)
        {
            string scriptPath = Path.Combine(workFolder, "update.ps1");

            // Mit BOM speichern, sonst liest Windows PowerShell 5.1 Umlaute falsch.
            File.WriteAllText(scriptPath, SwapScript, new UTF8Encoding(true));

            string arguments =
                $"-NoProfile -ExecutionPolicy Bypass -File {Quote(scriptPath)} " +
                $"-GamePid {gameProcessId} -Source {Quote(newBuildFolder)} -Target {Quote(gameFolder)} -Exe {Quote(exePath)}";

            Process.Start(new ProcessStartInfo("powershell.exe", arguments) { UseShellExecute = true });
        }

        // Anführungszeichen um einen Pfad. Ein "\" direkt vor dem schließenden Anführungszeichen
        // würde es maskieren, deshalb ohne abschließenden Backslash.
        private static string Quote(string path)
        {
            return "\"" + path.TrimEnd('\\', '/') + "\"";
        }

        // Wartet auf das Ende des Spiels, kopiert die neue Version darüber, startet neu.
        // robocopy statt Copy-Item: Es wiederholt bei kurz noch gesperrten Dateien und legt
        // Ordner sauber zusammen. Rückgabewert unter 8 heißt Erfolg.
        private const string SwapScript = @"param([int]$GamePid, [string]$Source, [string]$Target, [string]$Exe)
$Host.UI.RawUI.WindowTitle = 'Schutzhütte wird aktualisiert'
Write-Host 'Schutzhütte wird aktualisiert, bitte kurz warten ...'
Wait-Process -Id $GamePid -Timeout 30 -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500
robocopy $Source $Target /E /R:10 /W:1 /NP /NFL /NDL /NJH /NJS | Out-Null
if ($LASTEXITCODE -lt 8) {
    Write-Host 'Fertig. Das Spiel startet neu.'
    Start-Process -FilePath $Exe -WorkingDirectory $Target
    Remove-Item -Path (Split-Path $Source -Parent) -Recurse -Force -ErrorAction SilentlyContinue
} else {
    Write-Host ""Update fehlgeschlagen (robocopy $LASTEXITCODE). Die neue Version gibt es auch zum Herunterladen:""
    Write-Host 'https://github.com/desirzzzz/Schutzhuette/releases'
    Read-Host 'Enter zum Schließen'
    Start-Process -FilePath $Exe -WorkingDirectory $Target
}
";
    }
}
