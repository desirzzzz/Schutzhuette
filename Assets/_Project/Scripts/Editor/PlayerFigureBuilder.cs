using Schutzhuette.Player;
using UnityEditor;
using UnityEngine;

namespace Schutzhuette.EditorTools
{
    /// <summary>
    /// Ersetzt die graue Kapsel des Player-Prefabs durch eine einfache Figur aus Grundformen:
    /// Körper, Kopf mit Augen, Füße und zwei Arme an Schultergelenken. Gibt dem Prefab
    /// PlayerVisuals mit allen Verweisen. Collider und Netzwerk-Teile bleiben unverändert.
    /// </summary>
    /// <remarks>
    /// Maße relativ zur Kapselmitte der Figur (y = 0, Füße bei y = -1, Augenhöhe 0,7).
    /// Aufruf über das Menü "Schutzhütte/Spielfigur bauen" oder im Batchmode mit
    /// -executeMethod Schutzhuette.EditorTools.PlayerFigureBuilder.BuildFromCommandLine.
    /// Gibt es schon ein Kind "Figure", passiert nichts.
    /// </remarks>
    public static class PlayerFigureBuilder
    {
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
        private const string MaterialFolder = "Assets/_Project/Art/Materials";
        private const string LogPrefix = "[Figur-Setup] ";

        /// <summary>Menüeintrag im Editor.</summary>
        [MenuItem("Schutzhütte/Spielfigur bauen")]
        public static void BuildFromMenu()
        {
            string error = Build();
            uint hash = error == null ? PrototypeSceneBuilder.EnsureNetworkPrefabHash(PlayerPrefabPath) : 0;
            EditorUtility.DisplayDialog("Spielfigur", error ?? (hash != 0 ? "Fertig." : "Fertig, aber die Netzwerk-ID ist 0. Bitte melden."), "OK");
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

            uint hash = PrototypeSceneBuilder.EnsureNetworkPrefabHash(PlayerPrefabPath);
            if (hash == 0)
            {
                Debug.LogError(LogPrefix + "GlobalObjectIdHash des Player-Prefabs ist 0.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log(LogPrefix + $"Figur eingebaut. GlobalObjectIdHash: {hash}");
        }

        // Gibt null zurück, wenn alles geklappt hat, sonst eine Fehlermeldung.
        private static string Build()
        {
            Material skin = LoadOrCreateMaterial("Haut", new Color(0.95f, 0.8f, 0.65f));
            Material eyes = LoadOrCreateMaterial("Augen", new Color(0.05f, 0.05f, 0.05f));
            Material shoes = LoadOrCreateMaterial("Schuhe", new Color(0.2f, 0.18f, 0.16f));
            if (skin == null || eyes == null || shoes == null)
            {
                return "Materialien konnten nicht angelegt werden (URP-Lit-Shader nicht gefunden?).";
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                if (root.transform.Find("Figure") != null)
                {
                    return null;
                }

                // Die graue Kapsel nicht mehr zeigen. Ihr Collider bleibt (Kollision fremder Figuren).
                if (root.TryGetComponent(out MeshRenderer capsuleRenderer))
                {
                    Object.DestroyImmediate(capsuleRenderer);
                }

                if (root.TryGetComponent(out MeshFilter capsuleFilter))
                {
                    Object.DestroyImmediate(capsuleFilter);
                }

                var figure = new GameObject("Figure").transform;
                figure.SetParent(root.transform, false);

                // Körper 1,2 m hoch, unten bei -0,9; Kopf darüber mit der Kamera (0,7) innen.
                Renderer torso = AddPart(PrimitiveType.Capsule, "Torso", figure, new Vector3(0f, -0.3f, 0f), new Vector3(0.8f, 0.6f, 0.7f), null);
                Renderer head = AddPart(PrimitiveType.Sphere, "Head", figure, new Vector3(0f, 0.6f, 0f), Vector3.one * 0.55f, skin);
                Renderer eyeLeft = AddPart(PrimitiveType.Sphere, "EyeL", figure, new Vector3(-0.1f, 0.65f, 0.24f), Vector3.one * 0.1f, eyes);
                Renderer eyeRight = AddPart(PrimitiveType.Sphere, "EyeR", figure, new Vector3(0.1f, 0.65f, 0.24f), Vector3.one * 0.1f, eyes);
                AddPart(PrimitiveType.Cube, "FootL", figure, new Vector3(-0.18f, -0.94f, 0.05f), new Vector3(0.25f, 0.12f, 0.35f), shoes);
                AddPart(PrimitiveType.Cube, "FootR", figure, new Vector3(0.18f, -0.94f, 0.05f), new Vector3(0.25f, 0.12f, 0.35f), shoes);

                Transform leftPivot = AddArm("ArmPivotL", figure, new Vector3(-0.45f, 0.1f, 0f), out Renderer leftArm);
                Transform rightPivot = AddArm("ArmPivotR", figure, new Vector3(0.45f, 0.1f, 0f), out Renderer rightArm);

                var visuals = root.AddComponent<PlayerVisuals>();
                var serializedVisuals = new SerializedObject(visuals);
                serializedVisuals.FindProperty("_figure").objectReferenceValue = figure;
                serializedVisuals.FindProperty("_leftArmPivot").objectReferenceValue = leftPivot;
                serializedVisuals.FindProperty("_rightArmPivot").objectReferenceValue = rightPivot;
                SetArray(serializedVisuals.FindProperty("_headRenderers"), head, eyeLeft, eyeRight);
                SetArray(serializedVisuals.FindProperty("_colorRenderers"), torso, leftArm, rightArm);
                serializedVisuals.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                return null;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Grundform ohne Collider (die Figur ist nur Darstellung).
        private static Renderer AddPart(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;

            var partRenderer = part.GetComponent<Renderer>();
            if (material != null)
            {
                partRenderer.sharedMaterial = material;
            }

            return partRenderer;
        }

        // Schultergelenk mit Arm-Kapsel, die entlang der lokalen z-Achse zeigt (0,6 m lang).
        private static Transform AddArm(string name, Transform parent, Vector3 shoulder, out Renderer armRenderer)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = shoulder;
            pivot.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            arm.name = "Arm";
            Object.DestroyImmediate(arm.GetComponent<Collider>());
            arm.transform.SetParent(pivot, false);
            arm.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            arm.transform.localPosition = new Vector3(0f, 0f, 0.3f);
            arm.transform.localScale = new Vector3(0.16f, 0.3f, 0.16f);

            armRenderer = arm.GetComponent<Renderer>();
            return pivot;
        }

        private static void SetArray(SerializedProperty property, params Renderer[] renderers)
        {
            property.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }
        }

        private static Material LoadOrCreateMaterial(string name, Color baseColor)
        {
            string path = $"{MaterialFolder}/{name}.mat";
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

            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Materials");
            }

            material = new Material(lit);
            material.SetColor("_BaseColor", baseColor);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
