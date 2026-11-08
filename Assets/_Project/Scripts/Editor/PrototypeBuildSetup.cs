using UnityEditor;
using UnityEngine;

namespace Schutzhuette.EditorTools
{
    /// <summary>
    /// Stellt die Projekt-Einstellungen für den Prototyp-Build ein: nur die Szene
    /// Prototype im Build, Start im Fenster (1280 x 720, Größe änderbar), Fenstertitel
    /// und Version. Danach baut commands.build aus project.yaml das Spiel.
    /// </summary>
    /// <remarks>
    /// Warum Fenster statt Vollbild: Beim Testen soll man auch allein zwei Spiele
    /// nebeneinander öffnen können (eins als Host, eins tritt bei).
    /// Aufruf über das Menü "Schutzhütte/Build-Einstellungen für den Prototyp" oder im
    /// Batchmode mit -executeMethod Schutzhuette.EditorTools.PrototypeBuildSetup.ConfigureFromCommandLine.
    /// </remarks>
    public static class PrototypeBuildSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
        private const string ProductName = "Schutzhütte";
        private const string Version = "0.1.3";

        /// <summary>Menüeintrag im Editor.</summary>
        [MenuItem("Schutzhütte/Build-Einstellungen für den Prototyp")]
        public static void ConfigureFromMenu()
        {
            EditorUtility.DisplayDialog("Build-Einstellungen", Configure() ?? "Fertig.", "OK");
        }

        /// <summary>Batchmode-Einstieg. Beendet Unity mit Code 1, wenn etwas schiefgeht.</summary>
        public static void ConfigureFromCommandLine()
        {
            string error = Configure();
            if (error != null)
            {
                Debug.LogError("[Build-Setup] " + error);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[Build-Setup] Fertig: nur {ScenePath} im Build, Fenster 1280x720, {ProductName} {Version}.");
        }

        // Gibt null zurück, wenn alles geklappt hat, sonst eine Fehlermeldung.
        private static string Configure()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                return $"{ScenePath} fehlt.";
            }

            // Nur die Prototyp-Szene. Die erste Szene der Liste ist die Startszene.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;

            // Weiterlaufen, wenn das Fenster nicht vorne ist. NGO setzt das beim Start
            // einer Sitzung ohnehin, so gilt es auch schon im Hauptmenü.
            PlayerSettings.runInBackground = true;

            AssetDatabase.SaveAssets();
            return null;
        }
    }
}
