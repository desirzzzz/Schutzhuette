using Schutzhuette.Net;
using Schutzhuette.Player;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Schutzhuette.EditorTools
{
    /// <summary>
    /// Baut die Prototyp-Szene für den NGO-Schritt automatisch auf: Boden,
    /// Kamera, vier Spawnpunkte, Player-Prefab und NetworkManager mit allen
    /// Einstellungen.
    /// </summary>
    /// <remarks>
    /// Warum ein Editor-Skript: Szenen und Prefabs sollen nie als YAML von Hand
    /// bearbeitet werden. Hier schreibt Unity die Dateien selbst über seine API.
    /// Aufruf über das Menü "Schutzhütte/Prototyp-Szene einrichten" oder im
    /// Batchmode mit -executeMethod Schutzhuette.EditorTools.PrototypeSceneBuilder.BuildFromCommandLine.
    /// Überschreibt nichts: Gibt es Szene oder Prefab schon, bricht es ab.
    /// Nach dem ersten Lauf einmal RefreshPrefabHashFromCommandLine ausführen
    /// (oder Unity öffnen und File → Save Project), sonst bleibt die Netzwerk-ID
    /// des Player-Prefabs 0. Grund: siehe EnsureNetworkPrefabHash.
    /// </remarks>
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
        private const string LogPrefix = "[Setup] ";

        /// <summary>Menüeintrag im Editor. Fragt vorher nach ungespeicherten Szenen.</summary>
        [MenuItem("Schutzhütte/Prototyp-Szene einrichten")]
        public static void BuildFromMenu()
        {
            // Ungespeicherte Änderungen in der offenen Szene nicht stillschweigend verwerfen.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string error = Build();
            EditorUtility.DisplayDialog(
                "Prototyp-Szene",
                error ?? $"Fertig. Szene: {ScenePath}\nPrefab: {PlayerPrefabPath}",
                "OK");
        }

        /// <summary>
        /// Einstieg für den Batchmode (-executeMethod). Beendet Unity mit Code 1,
        /// wenn etwas schiefgeht, damit ein Aufrufer den Fehler am Exit-Code sieht.
        /// </summary>
        public static void BuildFromCommandLine()
        {
            string error = Build();
            if (error != null)
            {
                Debug.LogError(LogPrefix + error);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log(LogPrefix + $"Fertig. Szene: {ScenePath}, Prefab: {PlayerPrefabPath}");
        }

        /// <summary>
        /// Menüeintrag: erweitert das Player-Prefab um Laufen, Umschauen und
        /// Netzwerk-Synchronisierung der Position.
        /// </summary>
        [MenuItem("Schutzhütte/Player-Prefab: Bewegung einrichten")]
        public static void SetupPlayerMovementFromMenu()
        {
            string error = AddMovementToPlayerPrefab();
            uint hash = error == null ? EnsureNetworkPrefabHash(PlayerPrefabPath) : 0;
            EditorUtility.DisplayDialog(
                "Player-Prefab",
                error ?? (hash != 0 ? "Fertig." : "Fertig, aber die Netzwerk-ID ist 0. Bitte melden."),
                "OK");
        }

        /// <summary>
        /// Batchmode-Einstieg für <see cref="SetupPlayerMovementFromMenu"/>. Exit-Code 1 bei Fehler.
        /// </summary>
        public static void SetupPlayerMovementFromCommandLine()
        {
            string error = AddMovementToPlayerPrefab();
            if (error != null)
            {
                Debug.LogError(LogPrefix + error);
                EditorApplication.Exit(1);
                return;
            }

            uint hash = EnsureNetworkPrefabHash(PlayerPrefabPath);
            if (hash == 0)
            {
                Debug.LogError(LogPrefix + $"GlobalObjectIdHash von {PlayerPrefabPath} ist nach dem Speichern 0.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log(LogPrefix + $"Bewegung im Player-Prefab eingerichtet. GlobalObjectIdHash: {hash}");
        }

        // Gibt null zurück, wenn alles geklappt hat, sonst eine Fehlermeldung.
        private static string AddMovementToPlayerPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
            {
                return $"{PlayerPrefabPath} fehlt. Erst die Prototyp-Szene einrichten.";
            }

            // Prefab in einer unsichtbaren Bearbeitungs-Szene öffnen, ändern, speichern.
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                if (root.GetComponent<PlayerMovement>() != null)
                {
                    return "Das Player-Prefab hat schon PlayerMovement. Nichts geändert.";
                }

                // Gleiche Maße wie die Kapsel: 2 m hoch, Radius 0,5 m, Mitte im Ursprung.
                var controller = root.AddComponent<CharacterController>();
                controller.height = 2f;
                controller.radius = 0.5f;
                controller.center = Vector3.zero;

                // Owner-Autorität: Der Spieler bewegt seine Figur selbst, NGO verteilt die Pose.
                // Geteilt werden Position und Drehung um die Hochachse, sonst nichts.
                var networkTransform = root.AddComponent<NetworkTransform>();
                networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
                networkTransform.SyncRotAngleX = false;
                networkTransform.SyncRotAngleZ = false;
                networkTransform.SyncScaleX = false;
                networkTransform.SyncScaleY = false;
                networkTransform.SyncScaleZ = false;
                networkTransform.Interpolate = true;

                // Kopf-Punkt auf Augenhöhe (Kapselmitte + 0,7 m = 1,7 m über dem Boden).
                var cameraMount = new GameObject("CameraMount").transform;
                cameraMount.SetParent(root.transform, false);
                cameraMount.localPosition = new Vector3(0f, 0.7f, 0f);

                root.AddComponent<PlayerMovement>();
                var look = root.AddComponent<PlayerLook>();
                var serializedLook = new SerializedObject(look);
                serializedLook.FindProperty("_cameraMount").objectReferenceValue = cameraMount;
                serializedLook.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                return null;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Batchmode-Einstieg: Sorgt dafür, dass im Player-Prefab eine gültige
        /// Netzwerk-ID (GlobalObjectIdHash) gespeichert ist. Exit-Code 1, wenn nicht.
        /// </summary>
        public static void RefreshPrefabHashFromCommandLine()
        {
            uint hash = EnsureNetworkPrefabHash(PlayerPrefabPath);
            if (hash == 0)
            {
                Debug.LogError(LogPrefix + $"GlobalObjectIdHash von {PlayerPrefabPath} ist weiter 0.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log(LogPrefix + $"GlobalObjectIdHash von {PlayerPrefabPath}: {hash}");
        }

        // NGO berechnet die Netzwerk-ID eines Prefabs in NetworkObject.OnValidate,
        // sobald Unity das Prefab-Asset lädt, und markiert das Asset dann als
        // geändert. Ein Prefab, das gerade erst aus der Szene gespeichert wurde,
        // hat noch 0, und mit 0 verweigert NGO zur Laufzeit das Spawnen.
        // Deshalb: laden (dabei rechnet NGO die ID aus), ausdrücklich als geändert
        // markieren, speichern und in der Datei nachsehen, ob sie angekommen ist.
        internal static uint EnsureNetworkPrefabHash(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            NetworkObject networkObject = prefab != null ? prefab.GetComponent<NetworkObject>() : null;
            if (networkObject == null)
            {
                return 0;
            }

            // GlobalObjectIdHash ist in NGO nicht öffentlich, deshalb über SerializedObject lesen.
            SerializedProperty property = new SerializedObject(networkObject).FindProperty("GlobalObjectIdHash");
            uint hash = property != null ? (uint)property.longValue : 0;
            if (hash == 0)
            {
                return 0;
            }

            EditorUtility.SetDirty(networkObject);
            AssetDatabase.SaveAssets();

            // Nachweis auf der Platte, nicht nur im Speicher.
            string fileText = System.IO.File.ReadAllText(prefabPath);
            return fileText.Contains($"GlobalObjectIdHash: {hash}") ? hash : 0;
        }

        // Gibt null zurück, wenn alles geklappt hat, sonst eine Fehlermeldung.
        private static string Build()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(ScenePath) != null)
            {
                return $"{ScenePath} gibt es schon. Nichts geändert.";
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(PlayerPrefabPath) != null)
            {
                return $"{PlayerPrefabPath} gibt es schon. Nichts geändert.";
            }

            // Neue Szene mit Standard-Kamera und Licht.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject playerPrefab = CreatePlayerPrefab();
            if (playerPrefab == null)
            {
                return $"Prefab konnte nicht gespeichert werden: {PlayerPrefabPath}";
            }

            CreateGround();
            PlaceCamera(scene);
            Transform[] spawnPoints = CreateSpawnPoints();
            CreateNetworkManager(playerPrefab, spawnPoints);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                return $"Szene konnte nicht gespeichert werden: {ScenePath}";
            }

            AddSceneToBuildList(ScenePath);
            AssetDatabase.SaveAssets();
            return null;
        }

        // Kapsel mit NetworkObject als Prefab speichern, dann das Szenen-Objekt wieder löschen.
        private static GameObject CreatePlayerPrefab()
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Player";
            capsule.AddComponent<NetworkObject>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(capsule, PlayerPrefabPath);
            Object.DestroyImmediate(capsule);
            return prefab;
        }

        private static void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(2f, 1f, 2f); // 20 x 20 Meter
        }

        // Kamera schräg von oben, damit alle vier Spawnpunkte im Bild sind.
        private static void PlaceCamera(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<Camera>() != null)
                {
                    root.transform.SetPositionAndRotation(new Vector3(0f, 6f, -10f), Quaternion.Euler(30f, 0f, 0f));
                    return;
                }
            }
        }

        // Vier leere Objekte in einer Reihe. y = 1, weil die Kapsel 2 m hoch ist
        // und ihr Mittelpunkt dann genau auf Bodenhöhe + 1 m liegt.
        private static Transform[] CreateSpawnPoints()
        {
            float[] xPositions = { -3f, -1f, 1f, 3f };
            var parent = new GameObject("SpawnPoints").transform;
            var points = new Transform[xPositions.Length];

            for (int i = 0; i < xPositions.Length; i++)
            {
                var point = new GameObject($"Spawn{i + 1}").transform;
                point.SetParent(parent, false);
                point.localPosition = new Vector3(xPositions[i], 1f, 0f);
                points[i] = point;
            }

            return points;
        }

        private static void CreateNetworkManager(GameObject playerPrefab, Transform[] spawnPoints)
        {
            var go = new GameObject("NetworkManager");
            var networkManager = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();

            if (networkManager.NetworkConfig == null)
            {
                networkManager.NetworkConfig = new NetworkConfig();
            }

            NetworkConfig config = networkManager.NetworkConfig;
            config.NetworkTransport = transport;
            config.NetworkTopology = NetworkTopologyTypes.ClientServer;
            config.PlayerPrefab = playerPrefab;
            config.ConnectionApproval = true; // sonst ruft NGO PlayerSpawnApproval nie auf
            EditorUtility.SetDirty(networkManager);

            var approval = go.AddComponent<PlayerSpawnApproval>();
            go.AddComponent<ConnectionHud>();

            // _spawnPoints ist privat. SerializedObject setzt es so, wie es der Inspector täte.
            var serializedApproval = new SerializedObject(approval);
            SerializedProperty spawnProperty = serializedApproval.FindProperty("_spawnPoints");
            spawnProperty.arraySize = spawnPoints.Length;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                spawnProperty.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];
            }

            serializedApproval.ApplyModifiedPropertiesWithoutUndo();
        }

        // Szene in die Build-Liste aufnehmen, falls sie noch fehlt (für commands.build).
        private static void AddSceneToBuildList(string scenePath)
        {
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene entry in current)
            {
                if (entry.path == scenePath)
                {
                    return;
                }
            }

            var updated = new EditorBuildSettingsScene[current.Length + 1];
            current.CopyTo(updated, 0);
            updated[current.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = updated;
        }
    }
}
