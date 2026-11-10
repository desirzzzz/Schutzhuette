using System;
using System.Globalization;
using Schutzhuette.Net;
using UnityEngine;

namespace Schutzhuette.Core
{
    /// <summary>
    /// Schreibt beim Start und beim Beenden je eine Zeile mit Uhrzeit, Version und Build ins Log.
    /// Unity-Logs haben keine Uhrzeit. So lässt sich jeder Test einem Zeitpunkt und einer exe zuordnen,
    /// und die Spieldauer für Spieltest-Berichte steht gleich mit drin.
    /// </summary>
    /// <example>
    /// Im Log (Player.log) steht dann zum Beispiel:
    /// <code>
    /// [Sitzung] Start 10.11.2026 19:05:02, Version 0.1.4, Build 5c5799d5
    /// [Sitzung] Ende 10.11.2026 19:21:40, Dauer 16 min 38 s
    /// </code>
    /// </example>
    /// <remarks>
    /// Kein MonoBehaviour und kein Szenenobjekt: Unity ruft <see cref="OnStart"/> selbst auf.
    /// Das Abmelden vor dem Anmelden verhindert doppelte Zeilen, wenn man im Editor mehrmals Play drückt.
    /// </remarks>
    internal static class SessionLog
    {
        private const string Prefix = "[Sitzung] ";
        private const string TimeFormat = "dd.MM.yyyy HH:mm:ss";
        private const int BuildIdLength = 8;

        // Startzeit der laufenden Sitzung. Domain Reload ist aus, deshalb in ResetStatics zurückgesetzt.
        private static DateTime s_started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_started = default;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnStart()
        {
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;

            s_started = DateTime.Now;
            Debug.Log(Prefix + $"Start {Format(s_started)}, Version {GameVersion.Current}, {BuildLabel()}");
        }

        private static void OnQuitting()
        {
            Application.quitting -= OnQuitting;

            DateTime ended = DateTime.Now;
            TimeSpan duration = ended - s_started;
            Debug.Log(Prefix + $"Ende {Format(ended)}, Dauer {(int)duration.TotalMinutes} min {duration.Seconds} s");
        }

        private static string Format(DateTime time)
        {
            return time.ToString(TimeFormat, CultureInfo.InvariantCulture);
        }

        // In der exe die ersten Zeichen der Build-GUID, wie "build-guid" in Schutzhuette_Data/boot.config.
        // Im Editor gibt es keinen Build.
        private static string BuildLabel()
        {
            if (Application.isEditor)
            {
                return "Editor";
            }

            string guid = Application.buildGUID;
            return string.IsNullOrEmpty(guid)
                ? "Build unbekannt"
                : "Build " + guid.Substring(0, Math.Min(BuildIdLength, guid.Length));
        }
    }
}
