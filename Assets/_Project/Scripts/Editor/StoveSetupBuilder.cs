using Schutzhuette.Carrying;
using Schutzhuette.Hut;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Schutzhuette.EditorTools
{
    /// <summary>
    /// Richtet den Holzofen ein: zwei Materialien (Holz, Feuer), das Datenobjekt
    /// StoveSettings, die Prefabs Stove und Firewood und in der Prototyp-Szene je einen
    /// Spawner für Ofen und Holzplatz. Der graue Platzhalter-Ofen der Graubox wird entfernt.
    /// </summary>
    /// <remarks>
    /// Aufruf über das Menü "Schutzhütte/Ofen einrichten" oder im Batchmode mit
    /// -executeMethod Schutzhuette.EditorTools.StoveSetupBuilder.BuildFromCommandLine.
    /// Danach in einem eigenen Unity-Start RefreshPrefabHashesFromCommandLine ausführen,
    /// damit die neuen Prefabs eine Netzwerk-ID bekommen. Vorhandenes wird übersprungen.
    /// </remarks>
    public static class StoveSetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
        private const string MaterialFolder = "Assets/_Project/Art/Materials";
        private const string WoodMaterialPath = MaterialFolder + "/Holz.mat";
        private const string FireMaterialPath = MaterialFolder + "/Feuer.mat";
        private const string SettingsPath = "Assets/_Project/Data/StoveSettings.asset";
        private const string StovePrefabPath = "Assets/_Project/Prefabs/Stove.prefab";
        private const string FirewoodPrefabPath = "Assets/_Project/Prefabs/Firewood.prefab";
        private const string LogPrefix = "[Ofen-Setup] ";

        /// <summary>Menüeintrag im Editor. Fragt vorher nach ungespeicherten Szenen.</summary>
        [MenuItem("Schutzhütte/Ofen einrichten")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorUtility.DisplayDialog("Ofen", Build() ?? "Fertig. Jetzt Unity einmal neu öffnen und File → Save Project.", "OK");
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
        /// Batchmode-Einstieg: gültige Netzwerk-IDs für Ofen und Holzscheit. Muss in einem
        /// eigenen Unity-Start laufen, weil NGO die ID erst beim Laden ausrechnet.
        /// </summary>
        public static void RefreshPrefabHashesFromCommandLine()
        {
            foreach (string path in new[] { StovePrefabPath, FirewoodPrefabPath })
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
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Materials");
            }

            Material wood = LoadOrCreateMaterial(WoodMaterialPath, new Color(0.45f, 0.28f, 0.15f), null);
            Material fire = LoadOrCreateMaterial(FireMaterialPath, new Color(1f, 0.45f, 0.1f), new Color(4f, 1.6f, 0.3f));
            if (wood == null || fire == null)
            {
                return "Materialien konnten nicht angelegt werden (URP-Lit-Shader nicht gefunden?).";
            }

            StoveSettings settings = AssetDatabase.LoadAssetAtPath<StoveSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<StoveSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(FirewoodPrefabPath) == null && CreateFirewoodPrefab(wood) == null)
            {
                return $"Holz-Prefab konnte nicht gespeichert werden: {FirewoodPrefabPath}";
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(StovePrefabPath) == null && CreateStovePrefab(fire, settings) == null)
            {
                return $"Ofen-Prefab konnte nicht gespeichert werden: {StovePrefabPath}";
            }

            AssetDatabase.SaveAssets();
            return SetUpScene();
        }

        private static Material LoadOrCreateMaterial(string path, Color baseColor, Color? emission)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                return null;
            }

            material = new Material(lit);
            material.SetColor("_BaseColor", baseColor);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // Holzscheit: 0,2 x 0,2 x 0,6 m, 3 kg. Wird getragen wie eine Kiste (Carryable)
        // und ist Brennstoff (FuelItem). Netzwerk-Aufbau wie bei der Kiste.
        private static GameObject CreateFirewoodPrefab(Material wood)
        {
            GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cube);
            log.name = "Firewood";
            log.transform.localScale = new Vector3(0.2f, 0.2f, 0.6f);
            log.GetComponent<MeshRenderer>().sharedMaterial = wood;

            var body = log.AddComponent<Rigidbody>();
            body.mass = 3f;

            log.AddComponent<NetworkObject>();

            var networkTransform = log.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            networkTransform.SyncScaleX = false;
            networkTransform.SyncScaleY = false;
            networkTransform.SyncScaleZ = false;
            networkTransform.SwitchTransformSpaceWhenParented = true;

            log.AddComponent<NetworkRigidbody>();
            log.AddComponent<Carryable>();
            log.AddComponent<FuelItem>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(log, FirewoodPrefabPath);
            Object.DestroyImmediate(log);
            return prefab;
        }

        // Ofen: leeres Wurzel-Objekt mit Trigger vor der Ofentür (Holz-Einwurf), Kind "Body"
        // als fester Ofenkörper (1,4 x 1,2 x 1,4 m wie der Graubox-Platzhalter), Kind "Fire"
        // als glühendes Feuer in der Tür und Kind "FireLight" als warmes Punktlicht.
        // Kein Rigidbody: Der Ofen bewegt sich nie.
        private static GameObject CreateStovePrefab(Material fire, StoveSettings settings)
        {
            var root = new GameObject("Stove");

            var feedZone = root.AddComponent<BoxCollider>();
            feedZone.isTrigger = true;
            feedZone.center = new Vector3(0f, -0.1f, -0.85f);
            feedZone.size = new Vector3(1.2f, 1f, 0.6f);

            root.AddComponent<NetworkObject>();
            var stove = root.AddComponent<Stove>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(1.4f, 1.2f, 1.4f);

            GameObject fireVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fireVisual.name = "Fire";
            fireVisual.transform.SetParent(root.transform, false);
            fireVisual.transform.localPosition = new Vector3(0f, -0.15f, -0.71f);
            fireVisual.transform.localScale = new Vector3(0.6f, 0.35f, 0.05f);
            fireVisual.GetComponent<MeshRenderer>().sharedMaterial = fire;
            Object.DestroyImmediate(fireVisual.GetComponent<BoxCollider>());

            var lightObject = new GameObject("FireLight");
            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 0.3f, -1f);
            var fireLight = lightObject.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.55f, 0.2f);
            fireLight.range = 7f;
            fireLight.intensity = 3f;
            fireLight.shadows = LightShadows.None;

            var serializedStove = new SerializedObject(stove);
            serializedStove.FindProperty("_settings").objectReferenceValue = settings;
            serializedStove.FindProperty("_fireLight").objectReferenceValue = fireLight;
            serializedStove.FindProperty("_fireVisual").objectReferenceValue = fireVisual;
            serializedStove.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, StovePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static string SetUpScene()
        {
            // Erst die Szene öffnen, dann die Prefabs laden: OpenScene räumt nicht benutzte
            // Assets aus dem Speicher, vorher geladene Prefabs wären danach ungültig.
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var stovePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StovePrefabPath).GetComponent<NetworkObject>();
            var firewoodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirewoodPrefabPath).GetComponent<NetworkObject>();

            GameObject networkManager = null;
            Transform placeholderStove = null;
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.GetComponent<NetworkManager>() != null)
                {
                    networkManager = rootObject;
                }

                if (rootObject.name == "Huette")
                {
                    placeholderStove = rootObject.transform.Find("Einrichtung/Ofen");
                }
            }

            if (networkManager == null)
            {
                return $"In {ScenePath} gibt es keinen NetworkManager.";
            }

            // Der echte Ofen kommt zur Laufzeit an dieselbe Stelle, der graue Würfel geht.
            var stovePosition = new Vector3(2.2f, 2.1f, 20.9f);
            if (placeholderStove != null)
            {
                stovePosition = placeholderStove.position;
                Object.DestroyImmediate(placeholderStove.gameObject);
            }

            if (!HasSpawnerFor(networkManager, stovePrefab))
            {
                AddSpawner(networkManager, stovePrefab, "StoveSpawnPoint", new[] { stovePosition }, 0f, 0f);
            }

            if (!HasSpawnerFor(networkManager, firewoodPrefab))
            {
                // Holzplatz rechts neben dem Hangfuß: 2 Reihen mit je 3 Scheiten.
                Vector3[] woodPile =
                {
                    new Vector3(5f, 0.15f, 3.2f), new Vector3(5.5f, 0.15f, 3.2f), new Vector3(6f, 0.15f, 3.2f),
                    new Vector3(5f, 0.15f, 4f), new Vector3(5.5f, 0.15f, 4f), new Vector3(6f, 0.15f, 4f),
                };
                AddSpawner(networkManager, firewoodPrefab, "WoodPile", woodPile, 0f, 20f);
            }

            return EditorSceneManager.SaveScene(scene) ? null : $"Szene konnte nicht gespeichert werden: {ScenePath}";
        }

        private static bool HasSpawnerFor(GameObject networkManager, NetworkObject prefab)
        {
            foreach (CrateSpawner existing in networkManager.GetComponents<CrateSpawner>())
            {
                if (new SerializedObject(existing).FindProperty("_cratePrefab").objectReferenceValue == prefab)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddSpawner(GameObject networkManager, NetworkObject prefab, string pointsName,
            Vector3[] positions, float yRotation, float respawnDelay)
        {
            var parent = new GameObject(pointsName).transform;
            var points = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                var point = new GameObject($"{pointsName}{i + 1}").transform;
                point.SetParent(parent, false);
                point.SetPositionAndRotation(positions[i], Quaternion.Euler(0f, yRotation, 0f));
                points[i] = point;
            }

            var spawner = networkManager.AddComponent<CrateSpawner>();
            var serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("_cratePrefab").objectReferenceValue = prefab;
            serializedSpawner.FindProperty("_respawnDelay").floatValue = respawnDelay;
            SerializedProperty pointsProperty = serializedSpawner.FindProperty("_spawnPoints");
            pointsProperty.arraySize = points.Length;
            for (int i = 0; i < points.Length; i++)
            {
                pointsProperty.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            }

            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
