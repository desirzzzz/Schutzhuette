using System;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Schutzhuette.EditorTools
{
    /// <summary>
    /// "Farbe statt Grau": gibt Hütte, Gelände, Kisten, Truhe und Ofen eigene Materialien,
    /// setzt ein flaches Dach, einen Dielenboden und warme Lampen in die Hütte und stellt
    /// draußen kühles Abendlicht ein (blaue Sonne, dunkler Himmel, Nebel, Nachbearbeitung).
    /// </summary>
    /// <remarks>
    /// Aufruf über das Menü "Schutzhütte/Farbe und Licht setzen" oder im Batchmode mit
    /// -executeMethod Schutzhuette.EditorTools.HutLookBuilder.BuildFromCommandLine.
    /// Darf mehrmals laufen: Vorhandenes wird wiederverwendet und nur neu eingestellt.
    /// Alles hier ist feste Kulisse ohne NetworkObject. Jeder Rechner lädt dieselbe Szene,
    /// deshalb muss davon nichts über das Netz.
    /// Vorschaubilder ohne Play-Modus: -executeMethod
    /// Schutzhuette.EditorTools.HutLookBuilder.RenderPreviewsFromCommandLine -previewOut Ordner
    /// </remarks>
    public static class HutLookBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
        private const string MaterialFolder = "Assets/_Project/Art/Materials";
        private const string ProfilePath = "Assets/_Project/Art/Stimmung.asset";
        private const string SkyboxPath = MaterialFolder + "/Himmel.mat";
        private const string CratePrefabPath = "Assets/_Project/Prefabs/Crate.prefab";
        private const string ChestPrefabPath = "Assets/_Project/Prefabs/HeavyChest.prefab";
        private const string StovePrefabPath = "Assets/_Project/Prefabs/Stove.prefab";
        private const string FirewoodPrefabPath = "Assets/_Project/Prefabs/Firewood.prefab";
        private const string HutRootName = "Huette";
        private const string FitOutGroupName = "Ausbau";
        private const string VolumeName = "Stimmung";
        private const string LogPrefix = "[Look] ";

        /// <summary>Menüeintrag im Editor. Fragt vorher nach ungespeicherten Szenen.</summary>
        [MenuItem("Schutzhütte/Farbe und Licht setzen")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string error = Build();
            EditorUtility.DisplayDialog("Farbe und Licht", error ?? "Fertig.", "OK");
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

            Debug.Log(LogPrefix + "Fertig: Materialien, Dach, Boden, Lampen, Licht und Nachbearbeitung gesetzt.");
        }

        // Gibt null zurück, wenn alles geklappt hat, sonst eine Fehlermeldung.
        private static string Build()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (lit == null || skyShader == null)
            {
                return "Shader nicht gefunden (URP Lit oder Skybox/Procedural).";
            }

            // Materialien zuerst anlegen und speichern, dann die Szene öffnen.
            // OpenScene entlädt nicht benutzte Assets, danach neu laden.
            Material outerWood = SaveMaterial(lit, "Holz Aussen", new Color(0.62f, 0.48f, 0.36f), 0.1f);
            SaveMaterial(lit, "Holz Innen", new Color(0.85f, 0.72f, 0.56f), 0.2f);
            SaveMaterial(lit, "Dielen", new Color(0.6f, 0.45f, 0.32f), 0.25f);
            SaveMaterial(lit, "Stein", new Color(0.48f, 0.48f, 0.5f), 0.1f);
            SaveMaterial(lit, "Schnee", new Color(0.92f, 0.95f, 1f), 0.35f);
            SaveMaterial(lit, "Moebelholz", new Color(0.42f, 0.29f, 0.2f), 0.2f);
            SaveMaterial(lit, "Kistenholz", new Color(0.72f, 0.55f, 0.33f), 0.15f);
            SaveMaterial(lit, "Truhe", new Color(0.55f, 0.14f, 0.1f), 0.3f);
            SaveMaterial(lit, "Kacheln", new Color(0.22f, 0.4f, 0.3f), 0.55f);
            SaveMaterial(lit, "Lampe", new Color(1f, 0.9f, 0.7f), 0.2f, new Color(1.6f, 1.3f, 0.9f));
            SaveSkybox(skyShader);
            if (outerWood == null)
            {
                return $"Materialien konnten nicht in {MaterialFolder} angelegt werden.";
            }

            string prefabError = ApplyPrefabMaterials();
            if (prefabError != null)
            {
                return prefabError;
            }

            VolumeProfile profile = SaveVolumeProfile();
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject hut = FindRoot(scene, HutRootName);
            if (hut == null)
            {
                return $"In {ScenePath} gibt es kein \"{HutRootName}\". Erst die Graubox-Hütte bauen.";
            }

            ApplyHutMaterials(hut.transform);
            BuildFitOut(hut.transform);

            GameObject ground = FindRoot(scene, "Ground");
            if (ground != null)
            {
                // 200 x 200 m, damit der Rand im Nebel verschwindet.
                ground.transform.localScale = new Vector3(20f, 1f, 20f);
                SetMaterial(ground, "Schnee");
            }
            else
            {
                Debug.LogWarning(LogPrefix + "Kein Objekt \"Ground\" gefunden, Boden unverändert.");
            }

            string lightError = SetupOutdoorLight(scene);
            if (lightError != null)
            {
                return lightError;
            }

            SetupVolumeAndCameras(scene, AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath) ?? profile);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                return $"Szene konnte nicht gespeichert werden: {ScenePath}";
            }

            return null;
        }

        // ----- Materialien -----

        // Legt ein URP-Lit-Material an oder stellt ein vorhandenes neu ein.
        private static Material SaveMaterial(Shader lit, string name, Color baseColor, float smoothness, Color? emission = null)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = material == null;
            if (isNew)
            {
                material = new Material(lit);
            }

            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static Material LoadMaterial(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
        }

        // Dunkler Abendhimmel mit bläulichem Schimmer, ohne Sonnenscheibe.
        private static void SaveSkybox(Shader skyShader)
        {
            var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            bool isNew = sky == null;
            if (isNew)
            {
                sky = new Material(skyShader);
            }

            sky.SetColor("_SkyTint", new Color(0.38f, 0.45f, 0.62f));
            sky.SetColor("_GroundColor", new Color(0.32f, 0.34f, 0.4f));
            sky.SetFloat("_Exposure", 0.55f);
            sky.SetFloat("_AtmosphereThickness", 0.8f);
            sky.SetFloat("_SunDisk", 0f);
            sky.EnableKeyword("_SUNDISK_NONE");
            sky.DisableKeyword("_SUNDISK_SIMPLE");
            sky.DisableKeyword("_SUNDISK_HIGH_QUALITY");

            if (isNew)
            {
                AssetDatabase.CreateAsset(sky, SkyboxPath);
            }
            else
            {
                EditorUtility.SetDirty(sky);
            }
        }

        // Kiste, Truhe und Ofenkörper. Die Kopien der Vorhersage übernehmen das Material
        // der echten Gegenstände, dort muss nichts geändert werden.
        private static string ApplyPrefabMaterials()
        {
            string error = SetPrefabMaterial(CratePrefabPath, null, "Kistenholz");
            error ??= SetPrefabMaterial(ChestPrefabPath, null, "Truhe");
            error ??= SetPrefabMaterial(StovePrefabPath, "Body", "Kacheln");
            return error;
        }

        // Ändert nur das Material im Prefab-Asset, nichts sonst. Die Netzwerk-ID
        // (GlobalObjectIdHash) hängt am Asset und bleibt dabei gleich.
        private static string SetPrefabMaterial(string prefabPath, string childName, string materialName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Material material = LoadMaterial(materialName);
            if (prefab == null || material == null)
            {
                return $"{prefabPath} oder Material {materialName} nicht gefunden.";
            }

            Transform target = childName == null ? prefab.transform : prefab.transform.Find(childName);
            if (target == null || !target.TryGetComponent(out MeshRenderer meshRenderer))
            {
                return $"In {prefabPath} gibt es keinen MeshRenderer an \"{childName ?? prefab.name}\".";
            }

            meshRenderer.sharedMaterial = material;
            PrefabUtility.SavePrefabAsset(prefab);
            return null;
        }

        // Ordnet jedem Teil der Graubox nach seiner Gruppe ein Material zu.
        private static void ApplyHutMaterials(Transform hut)
        {
            foreach (MeshRenderer part in hut.GetComponentsInChildren<MeshRenderer>(true))
            {
                string group = part.transform.parent != null ? part.transform.parent.name : string.Empty;
                string materialName = group switch
                {
                    "Gelände" => part.name == "Sockel" ? "Stein" : "Schnee",
                    "Außenwände" => "Holz Aussen",
                    "Innenwände" => "Holz Innen",
                    "Einrichtung" => "Moebelholz",
                    _ => null,
                };

                if (materialName != null)
                {
                    SetMaterial(part.gameObject, materialName);
                }
            }
        }

        private static void SetMaterial(GameObject target, string materialName)
        {
            Material material = LoadMaterial(materialName);
            if (material != null && target.TryGetComponent(out MeshRenderer meshRenderer))
            {
                meshRenderer.sharedMaterial = material;
            }
        }

        // ----- Dach, Boden, Lampen -----

        private static void BuildFitOut(Transform hut)
        {
            Transform fitOut = hut.Find(FitOutGroupName);
            if (fitOut == null)
            {
                fitOut = new GameObject(FitOutGroupName).transform;
                fitOut.SetParent(hut, false);
            }

            // Flaches Dach auf den Wänden (Oberkante 4,3 m), 0,4 m Überstand. Mit Collider,
            // damit hochgeworfene Kisten an der Decke abprallen.
            EnsureCube(fitOut, "Dach", new Vector3(0f, 4.45f, 17f), new Vector3(12.8f, 0.3f, 10.8f), "Holz Innen", true);

            // Dielen nur als Belag auf dem Sockel, ohne Collider: Gelaufen wird weiter auf dem Sockel.
            EnsureCube(fitOut, "Dielenboden", new Vector3(0f, 1.51f, 17f), new Vector3(11.4f, 0.02f, 9.4f), "Dielen", false);

            // Hängelampen (Schirm 0,35 m unter der Decke): zwei in der Stube, je eine in Vorraum, Lager und Küche.
            EnsureLamp(fitOut, "Lampe Stube vorne", new Vector3(3f, 3.95f, 14.5f), 3f, 7f);
            EnsureLamp(fitOut, "Lampe Stube hinten", new Vector3(3f, 3.95f, 18.3f), 3f, 7f);
            EnsureLamp(fitOut, "Lampe Vorraum", new Vector3(-3.5f, 3.95f, 13.8f), 3f, 5f);
            EnsureLamp(fitOut, "Lampe Lager", new Vector3(-3.5f, 3.95f, 17f), 2.5f, 5f);
            EnsureLamp(fitOut, "Lampe Küche", new Vector3(-3.5f, 3.95f, 20.3f), 3f, 5f);
        }

        private static GameObject EnsureCube(Transform parent, string name, Vector3 position, Vector3 scale, string materialName, bool withCollider)
        {
            Transform existing = parent.Find(name);
            GameObject cube = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.SetPositionAndRotation(position, Quaternion.identity);
            cube.transform.localScale = scale;

            if (!withCollider && cube.TryGetComponent(out Collider cubeCollider))
            {
                Object.DestroyImmediate(cubeCollider);
            }

            SetMaterial(cube, materialName);
            return cube;
        }

        // Lampe = leuchtender Schirm (nur Darstellung) plus warmes Punktlicht darunter.
        // Ohne Schatten: Punktlicht-Schatten kosten viel und sind im Prototyp nicht nötig.
        private static void EnsureLamp(Transform parent, string name, Vector3 position, float intensity, float range)
        {
            Transform lamp = parent.Find(name);
            if (lamp == null)
            {
                lamp = new GameObject(name).transform;
                lamp.SetParent(parent, false);
            }

            lamp.SetPositionAndRotation(position, Quaternion.identity);

            GameObject shade = EnsureCube(lamp, "Schirm", position, new Vector3(0.35f, 0.18f, 0.35f), "Lampe", false);
            shade.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            Transform lightTransform = lamp.Find("Licht");
            if (lightTransform == null)
            {
                lightTransform = new GameObject("Licht").transform;
                lightTransform.SetParent(lamp, false);
            }

            lightTransform.SetPositionAndRotation(position + Vector3.down * 0.15f, Quaternion.identity);
            if (!lightTransform.TryGetComponent(out Light pointLight))
            {
                pointLight = lightTransform.gameObject.AddComponent<Light>();
            }

            pointLight.type = LightType.Point;
            pointLight.color = new Color(1f, 0.85f, 0.66f); // warmes Glühbirnenlicht
            pointLight.intensity = intensity;
            pointLight.range = range;
            pointLight.shadows = LightShadows.None;
        }

        // ----- Licht draußen -----

        // Kühles, tief stehendes Abendlicht, dunkler Himmel, bläulicher Nebel.
        // Das Dach hält dieses Licht aus der Hütte, drinnen leuchten nur Lampen und Ofen.
        private static string SetupOutdoorLight(Scene scene)
        {
            Light sun = null;
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                foreach (Light candidate in rootObject.GetComponentsInChildren<Light>(true))
                {
                    if (candidate.type == LightType.Directional)
                    {
                        sun = candidate;
                        break;
                    }
                }

                if (sun != null)
                {
                    break;
                }
            }

            if (sun == null)
            {
                return "Kein gerichtetes Licht (Directional Light) in der Szene gefunden.";
            }

            sun.color = new Color(0.62f, 0.72f, 0.95f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(28f, 35f, 0f);

            RenderSettings.sun = sun;
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);

            // Umgebungslicht aus drei festen Farben: braucht kein Licht-Backen.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.24f, 0.29f, 0.4f);
            RenderSettings.ambientEquatorColor = new Color(0.15f, 0.17f, 0.23f);
            RenderSettings.ambientGroundColor = new Color(0.07f, 0.07f, 0.09f);
            RenderSettings.reflectionIntensity = 0.5f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.45f, 0.52f, 0.65f);
            RenderSettings.fogDensity = 0.02f;
            return null;
        }

        // ----- Nachbearbeitung -----

        // Glühen für Feuer und Lampen, neutraler Tonwert, leichte Vignette.
        private static VolumeProfile SaveVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = VolumeProfileFactory.CreateVolumeProfileAtPath(ProfilePath);
            }

            Bloom bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.6f);

            Tonemapping tonemapping = GetOrAdd<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            Vignette vignette = GetOrAdd<Vignette>(profile);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.45f);

            ColorAdjustments colorAdjustments = GetOrAdd<ColorAdjustments>(profile);
            colorAdjustments.saturation.Override(0f);

            foreach (VolumeComponent component in profile.components)
            {
                EditorUtility.SetDirty(component);
            }

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            return profile.TryGet(out T component)
                ? component
                : VolumeProfileFactory.CreateVolumeComponent<T>(profile, false, false);
        }

        // Ein globales Volume mit dem Profil, und an jeder Kamera die Nachbearbeitung an.
        private static void SetupVolumeAndCameras(Scene scene, VolumeProfile profile)
        {
            GameObject volumeObject = FindRoot(scene, VolumeName);
            if (volumeObject == null)
            {
                volumeObject = new GameObject(VolumeName);
                SceneManager.MoveGameObjectToScene(volumeObject, scene);
            }

            if (!volumeObject.TryGetComponent(out Volume volume))
            {
                volume = volumeObject.AddComponent<Volume>();
            }

            volume.isGlobal = true;
            volume.priority = 1f;
            volume.weight = 1f;
            volume.sharedProfile = profile;

            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                foreach (Camera sceneCamera in rootObject.GetComponentsInChildren<Camera>(true))
                {
                    UniversalAdditionalCameraData cameraData = sceneCamera.GetUniversalAdditionalCameraData();
                    cameraData.renderPostProcessing = true;
                    EditorUtility.SetDirty(cameraData);
                }
            }
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == name)
                {
                    return rootObject;
                }
            }

            return null;
        }

        // ----- Vorschaubilder -----

        /// <summary>
        /// Batchmode: rendert ein paar feste Blickwinkel als PNG, ohne Play-Modus und ohne die
        /// Szene zu speichern. Kisten, Truhe, Holz und Ofen kommen sonst erst zur Laufzeit,
        /// deshalb werden sie nur für die Bilder an ihre Spawnpunkte gestellt.
        /// </summary>
        public static void RenderPreviewsFromCommandLine()
        {
            string outputFolder = GetArgument("-previewOut") ?? "Temp/HutPreview";
            Directory.CreateDirectory(outputFolder);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PlaceAtSpawnPoints(scene, StovePrefabPath, "StoveSpawnPoint");
            PlaceAtSpawnPoints(scene, CratePrefabPath, "CrateSpawnPoints");
            PlaceAtSpawnPoints(scene, ChestPrefabPath, "HeavySpawnPoints");
            PlaceAtSpawnPoints(scene, FirewoodPrefabPath, "WoodPile");

            var cameraObject = new GameObject("PreviewCamera");
            var previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.fieldOfView = 70f;
            previewCamera.nearClipPlane = 0.05f;
            previewCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            const int width = 1280;
            const int height = 720;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);

            // Das erste Bild nach dem Laden kommt im Batchmode ohne fertig eingerichtete Sonne heraus.
            // Deshalb einmal zum Aufwärmen rendern und das Bild gleich wieder überschreiben.
            RenderView(previewCamera, target, image, outputFolder, "1_Aussen", new Vector3(-6f, 3.2f, 1f), new Vector3(0f, 3f, 16f));

            // Augenhöhe einer Figur auf dem Hüttenboden: 1,5 + 1,7 = 3,2 m.
            RenderView(previewCamera, target, image, outputFolder, "1_Aussen", new Vector3(-6f, 3.2f, 1f), new Vector3(0f, 3f, 16f));
            RenderView(previewCamera, target, image, outputFolder, "2_Stube", new Vector3(5f, 3.2f, 12.7f), new Vector3(2.2f, 2.4f, 20.9f));
            RenderView(previewCamera, target, image, outputFolder, "3_Vorraum", new Vector3(-2f, 3.2f, 12.6f), new Vector3(-3.5f, 2.6f, 16f));
            RenderView(previewCamera, target, image, outputFolder, "4_Kueche", new Vector3(-2.2f, 3.2f, 18.9f), new Vector3(-3.6f, 2.2f, 21.3f));

            RenderTexture.active = null;
            Object.DestroyImmediate(cameraObject);
            Debug.Log(LogPrefix + $"Vorschaubilder gespeichert in {Path.GetFullPath(outputFolder)}");
        }

        private static void RenderView(Camera previewCamera, RenderTexture target, Texture2D image, string folder, string name, Vector3 position, Vector3 lookAt)
        {
            previewCamera.transform.position = position;
            previewCamera.transform.LookAt(lookAt);

            var request = new RenderPipeline.StandardRequest { destination = target };
            if (!RenderPipeline.SupportsRenderRequest(previewCamera, request))
            {
                Debug.LogError(LogPrefix + "Die Render-Pipeline unterstützt keine Render-Anfrage.");
                return;
            }

            RenderPipeline.SubmitRenderRequest(previewCamera, request);

            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
        }

        // Stellt eine Kopie des Prefabs an jeden Punkt unter dem Objekt mit diesem Namen.
        private static void PlaceAtSpawnPoints(Scene scene, string prefabPath, string pointsName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject points = FindRoot(scene, pointsName);
            if (prefab == null || points == null)
            {
                Debug.LogWarning(LogPrefix + $"Vorschau ohne {prefabPath} ({pointsName} nicht gefunden).");
                return;
            }

            foreach (Transform point in points.transform)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.SetPositionAndRotation(point.position, point.rotation);
            }
        }

        private static string GetArgument(string name)
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
    }
}
