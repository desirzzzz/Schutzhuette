using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Schutzhuette.EditorTools
{
    /// <summary>
    /// Richtet die simulierte Verzögerung ein: ein Network-Simulator-Preset für rund
    /// 100 ms Ping und eine NetworkSimulator-Komponente am NetworkManager der Prototyp-Szene.
    /// </summary>
    /// <remarks>
    /// Warum 25 ms pro Instanz ergeben rund 100 ms Ping: NGO richtet die Simulator-Stufe
    /// von Unity Transport für beide Richtungen ein (ApplyMode.AllPackets, Kommentar in
    /// UnityTransport.cs: "we handle both directions"). Jede Instanz verzögert also gesendete
    /// UND empfangene Pakete. In Multiplayer Play Mode haben Host und Client dieselbe Szene,
    /// also beide den Simulator: Hinweg 25 + 25 ms, Rückweg 25 + 25 ms = 100 ms.
    /// Ob das stimmt, zeigt die Ping-Anzeige im ConnectionHud.
    /// Aufruf über das Menü "Schutzhütte/Verzögerung (100 ms) einrichten" oder im Batchmode mit
    /// -executeMethod Schutzhuette.EditorTools.LatencySetupBuilder.BuildFromCommandLine.
    /// </remarks>
    public static class LatencySetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
        private const string PresetPath = "Assets/_Project/Data/Verzoegerung100ms.asset";
        private const string LogPrefix = "[Latenz-Setup] ";

        /// <summary>Menüeintrag im Editor. Fragt vorher nach ungespeicherten Szenen.</summary>
        [MenuItem("Schutzhütte/Verzögerung (100 ms) einrichten")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorUtility.DisplayDialog("Verzögerung", Build() ?? "Fertig.", "OK");
        }

        /// <summary>Batchmode-Einstieg. Beendet Unity mit Code 1, wenn etwas schiefgeht.</summary>
        public static void BuildFromCommandLine()
        {
            string error = Build();
            if (error != null)
            {
                Debug.LogError(LogPrefix + error);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log(LogPrefix + $"Fertig. Preset: {PresetPath}");
        }

        // Gibt null zurück, wenn alles geklappt hat, sonst eine Fehlermeldung.
        private static string Build()
        {
            // Erst die Szene öffnen, dann das Preset laden: OpenScene räumt nicht benutzte
            // Assets aus dem Speicher, ein vorher geladenes Preset wäre danach ungültig.
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var preset = AssetDatabase.LoadAssetAtPath<NetworkSimulatorPresetAsset>(PresetPath);
            if (preset == null)
            {
                preset = NetworkSimulatorPresetAsset.Create(
                    name: "Prototyp: rund 100 ms Ping",
                    description: "25 ms pro Instanz und Richtung, Host und Client zusammen rund 100 ms Rundlaufzeit.",
                    packetDelayMs: 25,
                    packetJitterMs: 5);
                AssetDatabase.CreateAsset(preset, PresetPath);
            }

            GameObject networkManager = null;
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.GetComponent<NetworkManager>() != null)
                {
                    networkManager = rootObject;
                    break;
                }
            }

            if (networkManager == null)
            {
                return $"In {ScenePath} gibt es keinen NetworkManager.";
            }

            // Laut Doku gehört der Simulator auf ein Objekt, das Szenenwechsel überlebt.
            // Der NetworkManager tut das.
            // Kein "??" hier: GetComponent liefert im Editor ein Unity-"null"-Objekt, das C# nicht als null erkennt.
            var simulator = networkManager.GetComponent<NetworkSimulator>();
            if (simulator == null)
            {
                simulator = networkManager.AddComponent<NetworkSimulator>();
            }

            // m_PresetAsset ist im Paket nicht öffentlich, aber serialisiert. Der Inspector
            // zeigt es als "Connection Preset: Custom" mit diesem Asset.
            var serializedSimulator = new SerializedObject(simulator);
            SerializedProperty presetProperty = serializedSimulator.FindProperty("m_PresetAsset");
            if (presetProperty == null)
            {
                return "NetworkSimulator hat kein Feld m_PresetAsset. Paketversion prüfen.";
            }

            presetProperty.objectReferenceValue = preset;
            serializedSimulator.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            return EditorSceneManager.SaveScene(scene) ? null : $"Szene konnte nicht gespeichert werden: {ScenePath}";
        }
    }
}
