using Schutzhuette.Carrying;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Schutzhuette.EditorTools
{
    /// <summary>
    /// Richtet das Tragen zu zweit ein: legt das Truhen-Prefab an, gibt dem Player-Prefab
    /// PlayerHeavyCarry und trägt einen zweiten CrateSpawner für die Truhe in die Szene ein.
    /// </summary>
    /// <remarks>
    /// Aufruf über das Menü "Schutzhütte/Tragen zu zweit einrichten" oder im Batchmode mit
    /// -executeMethod Schutzhuette.EditorTools.HeavyCarrySetupBuilder.BuildFromCommandLine.
    /// Danach in einem eigenen Unity-Start RefreshPrefabHashesFromCommandLine ausführen,
    /// damit das neue Prefab eine Netzwerk-ID bekommt. Vorhandenes wird übersprungen.
    /// </remarks>
    public static class HeavyCarrySetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
        private const string CratePrefabPath = "Assets/_Project/Prefabs/Crate.prefab";
        private const string HeavyPrefabPath = "Assets/_Project/Prefabs/HeavyChest.prefab";
        private const string LogPrefix = "[Heavy-Setup] ";

        /// <summary>Menüeintrag im Editor. Fragt vorher nach ungespeicherten Szenen.</summary>
        [MenuItem("Schutzhütte/Tragen zu zweit einrichten")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string error = Build();
            if (error == null)
            {
                PrototypeSceneBuilder.EnsureNetworkPrefabHash(HeavyPrefabPath);
            }

            EditorUtility.DisplayDialog("Tragen zu zweit", error ?? "Fertig.", "OK");
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

            Debug.Log(LogPrefix + "Fertig. Jetzt RefreshPrefabHashesFromCommandLine ausführen.");
        }

        /// <summary>
        /// Batchmode-Einstieg: gültige Netzwerk-IDs für Player, Kiste und Truhe.
        /// Muss in einem eigenen Unity-Start laufen, weil NGO die ID erst beim Laden ausrechnet.
        /// </summary>
        public static void RefreshPrefabHashesFromCommandLine()
        {
            foreach (string path in new[] { PlayerPrefabPath, CratePrefabPath, HeavyPrefabPath })
            {
                uint hash = PrototypeSceneBuilder.EnsureNetworkPrefabHash(path);
                if (hash == 0)
                {
                    Debug.LogError(LogPrefix + $"GlobalObjectIdHash von {path} ist 0.");
                    EditorApplication.Exit(1);
                    return;
                }

                Debug.Log(LogPrefix + $"GlobalObjectIdHash von {path}: {hash}");
            }
        }

        // Gibt null zurück, wenn alles geklappt hat, sonst eine Fehlermeldung.
        private static string Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HeavyPrefabPath) == null && CreateHeavyPrefab() == null)
            {
                return $"Truhen-Prefab konnte nicht gespeichert werden: {HeavyPrefabPath}";
            }

            string error = AddPlayerHeavyCarryToPlayerPrefab();
            if (error != null)
            {
                return error;
            }

            return AddHeavySpawnerToScene();
        }

        // Truhe: 0,6 m breit, 0,5 m hoch, 1,8 m lang (lange Seite = lokale z-Achse), 80 kg.
        // Gleicher Netzwerk-Aufbau wie die Kiste: Host rechnet, Clients sind kinematisch.
        private static GameObject CreateHeavyPrefab()
        {
            GameObject chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chest.name = "HeavyChest";
            chest.transform.localScale = new Vector3(0.6f, 0.5f, 1.8f);

            var body = chest.AddComponent<Rigidbody>();
            body.mass = 80f;

            chest.AddComponent<NetworkObject>();

            var networkTransform = chest.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            networkTransform.SyncScaleX = false;
            networkTransform.SyncScaleY = false;
            networkTransform.SyncScaleZ = false;

            chest.AddComponent<NetworkRigidbody>();
            chest.AddComponent<HeavyCarryable>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(chest, HeavyPrefabPath);
            Object.DestroyImmediate(chest);
            return prefab;
        }

        private static string AddPlayerHeavyCarryToPlayerPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                if (root.GetComponent<PlayerHeavyCarry>() != null)
                {
                    return null;
                }

                Transform cameraMount = root.transform.Find("CameraMount");
                if (cameraMount == null)
                {
                    return "Im Player-Prefab fehlt CameraMount. Erst die Bewegung einrichten.";
                }

                var heavyCarry = root.AddComponent<PlayerHeavyCarry>();
                var serializedHeavyCarry = new SerializedObject(heavyCarry);
                serializedHeavyCarry.FindProperty("_cameraMount").objectReferenceValue = cameraMount;
                serializedHeavyCarry.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                return null;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string AddHeavySpawnerToScene()
        {
            // Erst die Szene öffnen, dann das Prefab laden: OpenScene räumt nicht benutzte
            // Assets aus dem Speicher, ein vorher geladenes Prefab wäre danach ungültig.
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var heavyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeavyPrefabPath).GetComponent<NetworkObject>();

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

            // Gibt es schon einen Spawner für die Truhe? Dann nichts tun.
            foreach (CrateSpawner existing in networkManager.GetComponents<CrateSpawner>())
            {
                if (new SerializedObject(existing).FindProperty("_cratePrefab").objectReferenceValue == heavyPrefab)
                {
                    return null;
                }
            }

            // Rechts neben dem Hangfuß auf flachem Boden, quer liegend.
            var parent = new GameObject("HeavySpawnPoints").transform;
            var point = new GameObject("Heavy1").transform;
            point.SetParent(parent, false);
            point.SetPositionAndRotation(new Vector3(5f, 0.3f, 1.5f), Quaternion.Euler(0f, 90f, 0f));

            var spawner = networkManager.AddComponent<CrateSpawner>();
            var serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("_cratePrefab").objectReferenceValue = heavyPrefab;
            SerializedProperty pointsProperty = serializedSpawner.FindProperty("_spawnPoints");
            pointsProperty.arraySize = 1;
            pointsProperty.GetArrayElementAtIndex(0).objectReferenceValue = point;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

            return EditorSceneManager.SaveScene(scene) ? null : $"Szene konnte nicht gespeichert werden: {ScenePath}";
        }
    }
}
