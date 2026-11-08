using System.Collections.Generic;
using System.IO;
using Schutzhuette.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Schutzhuette.EditorTools
{
    /// <summary>
    /// Richtet die Geräusche ein: Import-Einstellungen der Sounds in Art/Audio, das Datenobjekt
    /// SoundSettings mit allen Clips, die Geräusch-Komponenten an Kiste, Truhe, Holzscheit,
    /// Spielfigur und Ofen und in der Prototyp-Szene ein Objekt für Wind und Stummtaste.
    /// </summary>
    /// <remarks>
    /// Aufruf über das Menü "Schutzhütte/Geräusche einrichten" oder im Batchmode mit
    /// -executeMethod Schutzhuette.EditorTools.SoundSetupBuilder.BuildFromCommandLine.
    /// Darf mehrmals laufen: Vorhandenes wird wiederverwendet. Die Zahlen im Datenobjekt
    /// (Lautstärken usw.) bleiben dabei, wie sie sind, nur die Clips werden neu eingetragen.
    /// Die Prefabs behalten ihre Netzwerk-ID (GlobalObjectIdHash), die hängt am Asset.
    /// </remarks>
    public static class SoundSetupBuilder
    {
        private const string AudioFolder = "Assets/_Project/Art/Audio";
        private const string SettingsPath = "Assets/_Project/Data/SoundSettings.asset";
        private const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
        private const string CratePrefabPath = "Assets/_Project/Prefabs/Crate.prefab";
        private const string ChestPrefabPath = "Assets/_Project/Prefabs/HeavyChest.prefab";
        private const string FirewoodPrefabPath = "Assets/_Project/Prefabs/Firewood.prefab";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
        private const string StovePrefabPath = "Assets/_Project/Prefabs/Stove.prefab";
        private const string AmbienceName = "Umgebungsgeräusche";
        private const string LogPrefix = "[Geräusche-Setup] ";

        /// <summary>Menüeintrag im Editor. Fragt vorher nach ungespeicherten Szenen.</summary>
        [MenuItem("Schutzhütte/Geräusche einrichten")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorUtility.DisplayDialog("Geräusche", Build() ?? "Fertig.", "OK");
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

            Debug.Log(LogPrefix + "Fertig: Sounds, SoundSettings, Prefabs und Szene eingerichtet.");
        }

        // Gibt null zurück, wenn alles geklappt hat, sonst eine Fehlermeldung.
        private static string Build()
        {
            Dictionary<string, AudioClip> clips = ImportClips();
            if (clips.Count == 0)
            {
                return $"Keine Sounds in {AudioFolder} gefunden.";
            }

            SoundSettings settings = AssetDatabase.LoadAssetAtPath<SoundSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<SoundSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            string error = FillSettings(settings, clips);
            error ??= AddToPrefab<ImpactSound>(CratePrefabPath, settings);
            error ??= AddToPrefab<ImpactSound>(ChestPrefabPath, settings);
            error ??= AddToPrefab<ImpactSound>(FirewoodPrefabPath, settings);
            error ??= AddToPrefab<FootstepSound>(PlayerPrefabPath, settings);
            error ??= AddToPrefab<StoveSound>(StovePrefabPath, settings);
            if (error != null)
            {
                return error;
            }

            AssetDatabase.SaveAssets();
            return SetUpScene(settings);
        }

        // Stellt den Import jedes Sounds ein und gibt alle Clips nach Dateiname zurück.
        // Kurze Geräusche werden beim Laden entpackt (schnell abspielbar), lange Schleifen
        // bleiben gepackt im Speicher. Alles, was im Raum klingt, wird Mono, nur der Wind
        // (überall gleich zu hören) bleibt Stereo.
        private static Dictionary<string, AudioClip> ImportClips()
        {
            var clips = new Dictionary<string, AudioClip>();
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);

                if (AssetImporter.GetAtPath(path) is AudioImporter importer)
                {
                    bool isLoop = name.EndsWith("_loop");
                    AudioImporterSampleSettings sample = importer.defaultSampleSettings;
                    sample.loadType = isLoop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                    sample.compressionFormat = AudioCompressionFormat.Vorbis;
                    importer.defaultSampleSettings = sample;
                    importer.forceToMono = name != "wind_loop";
                    importer.SaveAndReimport();
                }

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    clips[name] = clip;
                }
            }

            return clips;
        }

        private static string FillSettings(SoundSettings settings, Dictionary<string, AudioClip> clips)
        {
            var serialized = new SerializedObject(settings);

            string error = SetClipArray(serialized, "_impactClips", clips, "impactWood_heavy_");
            error ??= SetClipArray(serialized, "_heavyImpactClips", clips, "impactSoft_heavy_");
            error ??= SetClipArray(serialized, "_woodSteps", clips, "footstep_wood_");
            error ??= SetClipArray(serialized, "_snowSteps", clips, "footstep_snow_");
            error ??= SetClip(serialized, "_stoveLoop", clips, "fire_loop");
            error ??= SetClip(serialized, "_windLoop", clips, "wind_loop");
            if (error != null)
            {
                return error;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            return null;
        }

        private static string SetClipArray(SerializedObject serialized, string property, Dictionary<string, AudioClip> clips, string prefix)
        {
            var matching = new List<string>();
            foreach (string name in clips.Keys)
            {
                if (name.StartsWith(prefix))
                {
                    matching.Add(name);
                }
            }

            if (matching.Count == 0)
            {
                return $"Keine Sounds mit \"{prefix}\" in {AudioFolder}.";
            }

            matching.Sort(System.StringComparer.Ordinal);
            SerializedProperty array = serialized.FindProperty(property);
            array.arraySize = matching.Count;
            for (int i = 0; i < matching.Count; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = clips[matching[i]];
            }

            return null;
        }

        private static string SetClip(SerializedObject serialized, string property, Dictionary<string, AudioClip> clips, string name)
        {
            if (!clips.TryGetValue(name, out AudioClip clip))
            {
                return $"Sound \"{name}\" fehlt in {AudioFolder}.";
            }

            serialized.FindProperty(property).objectReferenceValue = clip;
            return null;
        }

        // Hängt eine Komponente an die Wurzel des Prefabs (falls noch nicht da) und trägt
        // die Geräusch-Einstellungen ein.
        private static string AddToPrefab<T>(string prefabPath, SoundSettings settings) where T : Component
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                return $"{prefabPath} nicht gefunden.";
            }

            try
            {
                if (!root.TryGetComponent(out T component))
                {
                    component = root.AddComponent<T>();
                }

                var serialized = new SerializedObject(component);
                serialized.FindProperty("_settings").objectReferenceValue = settings;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool saved);
                return saved ? null : $"{prefabPath} konnte nicht gespeichert werden.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Ein festes Szenen-Objekt für Wind und Stummtaste. Kein NetworkObject: Jeder Rechner
        // lädt dieselbe Szene und spielt seinen Wind selbst.
        private static string SetUpScene(SoundSettings settings)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject ambience = null;
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == AmbienceName)
                {
                    ambience = rootObject;
                }
            }

            if (ambience == null)
            {
                ambience = new GameObject(AmbienceName);
                SceneManager.MoveGameObjectToScene(ambience, scene);
            }

            if (!ambience.TryGetComponent(out WindSound wind))
            {
                wind = ambience.AddComponent<WindSound>();
            }

            var serializedWind = new SerializedObject(wind);
            serializedWind.FindProperty("_settings").objectReferenceValue = settings;
            serializedWind.ApplyModifiedPropertiesWithoutUndo();

            if (!ambience.TryGetComponent(out AudioMuteToggle _))
            {
                ambience.AddComponent<AudioMuteToggle>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            return EditorSceneManager.SaveScene(scene) ? null : $"Szene konnte nicht gespeichert werden: {ScenePath}";
        }
    }
}
