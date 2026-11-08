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
    /// Richtet das Kisten-Tragen ein: legt das Kisten-Prefab an, gibt dem Player-Prefab
    /// PlayerCarry und trägt den CrateSpawner mit drei Startpunkten in die Prototyp-Szene ein.
    /// </summary>
    /// <remarks>
    /// Aufruf über das Menü "Schutzhütte/Kisten-Tragen einrichten" oder im Batchmode mit
    /// -executeMethod Schutzhuette.EditorTools.CarrySetupBuilder.BuildFromCommandLine.
    /// Danach einmal RefreshPrefabHashesFromCommandLine ausführen (oder Unity öffnen und
    /// File → Save Project), damit das neue Kisten-Prefab eine Netzwerk-ID bekommt.
    /// Bereits vorhandene Teile werden übersprungen, nichts wird überschrieben.
    /// </remarks>
    public static class CarrySetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
        private const string CratePrefabPath = "Assets/_Project/Prefabs/Crate.prefab";
        private const string LogPrefix = "[Carry-Setup] ";

        /// <summary>Menüeintrag im Editor. Fragt vorher nach ungespeicherten Szenen.</summary>
        [MenuItem("Schutzhütte/Kisten-Tragen einrichten")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string error = Build();
            if (error == null)
            {
                PrototypeSceneBuilder.EnsureNetworkPrefabHash(CratePrefabPath);
            }

            EditorUtility.DisplayDialog("Kisten-Tragen", error ?? "Fertig.", "OK");
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
        /// Batchmode-Einstieg: sorgt für gültige Netzwerk-IDs in Player- und Kisten-Prefab.
        /// Muss in einem eigenen Unity-Start laufen, weil NGO die ID erst beim Laden ausrechnet.
        /// </summary>
        public static void RefreshPrefabHashesFromCommandLine()
        {
            foreach (string path in new[] { PlayerPrefabPath, CratePrefabPath })
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
            GameObject cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CratePrefabPath) ?? CreateCratePrefab();
            if (cratePrefab == null)
            {
                return $"Kisten-Prefab konnte nicht gespeichert werden: {CratePrefabPath}";
            }

            string error = AddPlayerCarryToPlayerPrefab();
            if (error != null)
            {
                return error;
            }

            return AddCrateSpawnerToScene(cratePrefab.GetComponent<NetworkObject>());
        }

        // Kiste: 0,6-m-Würfel, 10 kg. Der Host rechnet die Physik (NetworkTransform auf Server),
        // NetworkRigidbody macht die Kiste auf den Clients kinematisch, damit dort nichts dagegen rechnet.
        private static GameObject CreateCratePrefab()
        {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Crate";
            crate.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            var body = crate.AddComponent<Rigidbody>();
            body.mass = 10f;

            crate.AddComponent<NetworkObject>();

            var networkTransform = crate.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            networkTransform.SyncScaleX = false;
            networkTransform.SyncScaleY = false;
            networkTransform.SyncScaleZ = false;
            // Beim Aufheben wird die Kiste Kind der Spielfigur. Diese Einstellung lässt
            // NetworkTransform dann selbst in lokale Koordinaten wechseln und zurück.
            networkTransform.SwitchTransformSpaceWhenParented = true;

            crate.AddComponent<NetworkRigidbody>();
            crate.AddComponent<Carryable>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(crate, CratePrefabPath);
            Object.DestroyImmediate(crate);
            return prefab;
        }

        private static string AddPlayerCarryToPlayerPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                if (root.GetComponent<PlayerCarry>() != null)
                {
                    return null;
                }

                Transform cameraMount = root.transform.Find("CameraMount");
                if (cameraMount == null)
                {
                    return "Im Player-Prefab fehlt CameraMount. Erst die Bewegung einrichten.";
                }

                var carry = root.AddComponent<PlayerCarry>();
                var serializedCarry = new SerializedObject(carry);
                serializedCarry.FindProperty("_cameraMount").objectReferenceValue = cameraMount;
                serializedCarry.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                return null;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string AddCrateSpawnerToScene(NetworkObject cratePrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

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

            if (networkManager.GetComponent<CrateSpawner>() != null)
            {
                return null;
            }

            // Drei Startplätze links am Fuß des Hangs, je 1 m auseinander.
            Vector3[] positions =
            {
                new Vector3(-6f, 0.35f, 0.8f),
                new Vector3(-5f, 0.35f, 0.8f),
                new Vector3(-5.5f, 0.35f, 1.6f),
            };

            var parent = new GameObject("CrateSpawnPoints").transform;
            var points = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                var point = new GameObject($"Crate{i + 1}").transform;
                point.SetParent(parent, false);
                point.position = positions[i];
                points[i] = point;
            }

            var spawner = networkManager.AddComponent<CrateSpawner>();
            var serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("_cratePrefab").objectReferenceValue = cratePrefab;
            SerializedProperty pointsProperty = serializedSpawner.FindProperty("_spawnPoints");
            pointsProperty.arraySize = points.Length;
            for (int i = 0; i < points.Length; i++)
            {
                pointsProperty.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            }

            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

            return EditorSceneManager.SaveScene(scene) ? null : $"Szene konnte nicht gespeichert werden: {ScenePath}";
        }
    }
}
