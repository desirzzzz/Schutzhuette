using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Schutzhuette.EditorTools
{
    /// <summary>
    /// Formt den Berg rund um die Hütte als Unity Terrain und ersetzt damit die flache Bodenplatte:
    /// flacher Hüttenplatz auf Höhe 0, davor ein Hang ins Tal, dahinter und an den Seiten Grate,
    /// die zu steil zum Hochlaufen sind (natürlicher Kartenrand). Steile Stellen sind Fels, der
    /// Rest Schnee mit wenig Reibung, damit Kisten am Hang rutschen.
    /// </summary>
    /// <remarks>
    /// Aufruf über das Menü "Schutzhütte/Berg formen" oder im Batchmode mit
    /// -executeMethod Schutzhuette.EditorTools.HutTerrainBuilder.BuildFromCommandLine.
    /// Danach baut es die Erweiterung (HutExtensionBuilder) neu, damit Talstation, Seil und
    /// Suchgebiet auf dem Gelände stehen. Darf mehrmals laufen.
    /// Das Gelände wird einmal hier im Editor geformt und liegt dann fest in der Szene. Jeder Rechner
    /// lädt dieselbe Szene, deshalb muss davon nichts über das Netz. Rutschende Kisten berechnet
    /// wie bisher der Host. Maße und Werte: docs/GRAUBOX.md, Abschnitt "Gelände".
    /// </remarks>
    public static class HutTerrainBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
        private const string ArtFolder = "Assets/_Project/Art";
        private const string TerrainFolder = ArtFolder + "/Terrain";
        private const string TerrainDataPath = TerrainFolder + "/Berg.asset";
        private const string TerrainMaterialPath = ArtFolder + "/Materials/Gelaende.mat";
        private const string TerrainName = "Berg";
        private const string LogPrefix = "[Berg] ";

        // Lage des Geländes: x -120 bis 120, z -110 bis 130, Höhen -30 bis 110 m.
        private const float OriginX = -120f;
        private const float OriginZ = -110f;
        private const float BaseY = -30f;
        private const float SizeXZ = 240f;
        private const float HeightRange = 140f;
        private const int HeightResolution = 513; // rund 0,47 m je Messpunkt
        private const int AlphaResolution = 256;  // rund 0,94 m je Farbpunkt

        // Hüttenplatz: hier bleibt alles exakt auf Höhe 0, wie bisher auf der Bodenplatte.
        private const float ShelfMinX = -22f;
        private const float ShelfMaxX = 20f;
        private const float ShelfMinZ = -3f;
        private const float ShelfMaxZ = 28f;

        // Hang ins Tal: 0,45 m Abstieg je Meter, das sind rund 24 Grad (begehbar bis 45 Grad).
        private const float ValleySlope = 0.45f;
        private const float ValleyDepth = 22f;

        /// <summary>Mitte der Talstation (x). Dort wird ein ebener Absatz in den Hang gelegt.</summary>
        internal const float ValleyStationX = -11.25f;

        /// <summary>Mitte der Talstation (z), 12 m weiter vorn als in der Skizze.</summary>
        internal const float ValleyStationZ = -18f;

        // Ebener Absatz der Talstation (halbe Breite x, halbe Tiefe z), etwas größer als die Laderampe.
        private const float StationHalfX = 4.75f;
        private const float StationHalfZ = 3.5f;

        // Böschung rund um den Absatz: höchstens 0,75 m je Meter (rund 37 Grad), also begehbar.
        // Nur bis 8 m Abstand, sonst würde sie auch die fernen Grate kappen.
        private const float StationBankSlope = 0.75f;
        private const float StationBankReach = 8f;

        // Spielbereich (x, z) für die Steilheits-Meldung im Log.
        private const float PlayMinX = -26f;
        private const float PlayMaxX = 24f;
        private const float PlayMinZ = -24f;
        private const float PlayMaxZ = 32f;

        // Schnee: wenig Reibung, "Minimum" heißt, die kleinere Reibung der beiden Körper gilt.
        // Ab etwa 17 Grad Neigung rutscht eine Kiste los (tan 17° ≈ 0,3).
        private const float SnowStaticFriction = 0.3f;
        private const float SnowDynamicFriction = 0.25f;

        /// <summary>Menüeintrag im Editor. Fragt vorher nach ungespeicherten Szenen.</summary>
        [MenuItem("Schutzhütte/Berg formen")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string error = Build() ?? HutExtensionBuilder.Build(out _);
            EditorUtility.DisplayDialog("Berg formen", error ?? "Fertig.", "OK");
        }

        /// <summary>Batchmode-Einstieg. Beendet Unity mit Code 1, wenn etwas schiefgeht.</summary>
        public static void BuildFromCommandLine()
        {
            string error = Build() ?? HutExtensionBuilder.Build(out _);
            if (error != null)
            {
                Debug.LogError(LogPrefix + error);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log(LogPrefix + "Fertig: Gelände geformt, Erweiterung auf das Gelände gesetzt.");
        }

        // Gibt null zurück, wenn alles geklappt hat, sonst eine Fehlermeldung.
        private static string Build()
        {
            Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (terrainShader == null)
            {
                return "Shader \"Universal Render Pipeline/Terrain/Lit\" nicht gefunden.";
            }

            // Erst die Szene öffnen, dann die Assets anlegen: OpenScene entlädt nicht benutzte Assets.
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!AssetDatabase.IsValidFolder(TerrainFolder))
            {
                AssetDatabase.CreateFolder(ArtFolder, "Terrain");
            }

            TerrainLayer snow = SaveLayer("Schnee", SaveGrainTexture("Schnee Textur", new Color32(225, 232, 245, 255), 6, 4, 11), 8f, 0.3f);
            // Fels mit wenig Kontrast und großer Kachel, sonst wirkt die Wiederholung von weitem wie ein Gitter.
            TerrainLayer rock = SaveLayer("Fels", SaveGrainTexture("Fels Textur", new Color32(108, 108, 114, 255), 9, 5, 23), 10f, 0.1f);
            PhysicsMaterial snowPhysics = SaveSnowPhysics();
            Material terrainMaterial = SaveTerrainMaterial(terrainShader);
            TerrainData data = SaveTerrainData(snow, rock);
            LogSteepestPlayArea(data);

            // Die alte Bodenplatte wird vom Gelände ersetzt.
            GameObject ground = FindRoot(scene, "Ground");
            if (ground != null)
            {
                Object.DestroyImmediate(ground);
            }

            GameObject terrainObject = FindRoot(scene, TerrainName);
            if (terrainObject == null)
            {
                terrainObject = Terrain.CreateTerrainGameObject(data);
                terrainObject.name = TerrainName;
            }

            terrainObject.transform.SetPositionAndRotation(new Vector3(OriginX, BaseY, OriginZ), Quaternion.identity);
            var terrain = terrainObject.GetComponent<Terrain>();
            terrain.terrainData = data;
            terrain.materialTemplate = terrainMaterial;
            terrain.drawInstanced = true;

            var terrainCollider = terrainObject.GetComponent<TerrainCollider>();
            terrainCollider.terrainData = data;
            terrainCollider.sharedMaterial = snowPhysics;

            // Etwas lichterer Nebel als bisher, damit die Berge nicht ganz darin verschwinden.
            RenderSettings.fogDensity = HutLookBuilder.FogDensity;

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                return $"Szene konnte nicht gespeichert werden: {ScenePath}";
            }

            return null;
        }

        // ----- Höhen -----

        // Rohe Geländehöhe in Metern an einer Weltposition, vor Glätten und Einebnen.
        private static float RawHeight(float x, float z)
        {
            float height = 0f;

            // Vorne: Hang ins Tal bis zum Talboden, ganz vorne ein steiler Gegenhang als Rand.
            if (z < ShelfMinZ)
            {
                height -= Mathf.Min((ShelfMinZ - z) * ValleySlope, ValleyDepth);
            }

            if (z < -75f)
            {
                height += (-75f - z) * 1.3f;
            }

            // Hinten: 8 m sanft ansteigend (rund 20 Grad), dann steil (über 50 Grad, nicht begehbar).
            if (z > ShelfMaxZ)
            {
                height += Mathf.Min(z - ShelfMaxZ, 8f) * 0.375f;
            }

            if (z > ShelfMaxZ + 8f)
            {
                height += (z - ShelfMaxZ - 8f) * 1.4f;
            }

            // Seiten: Grate, erst begehbar (rund 31 Grad), weiter außen zu steil.
            height += SideRise(ShelfMinX - x) + SideRise(x - ShelfMaxX);

            // Außerhalb der Spielfläche etwas Unruhe, auf den Bergen mehr als im Tal.
            float outside = DistanceOutside(x, z, PlayMinX, PlayMaxX, PlayMinZ, PlayMaxZ);
            float mask = Mathf.SmoothStep(0f, 1f, outside / 15f);
            if (mask > 0f)
            {
                float amplitude = 2.5f + 0.12f * Mathf.Max(0f, height);
                float noise = (Mathf.PerlinNoise(x * 0.035f + 31.7f, z * 0.035f + 12.3f) - 0.5f) * 2f * amplitude
                    + (Mathf.PerlinNoise(x * 0.09f + 5.1f, z * 0.09f + 77.7f) - 0.5f) * 1.2f;
                height += noise * mask;
            }

            // Gipfel weich deckeln, damit das Gelände in seinen Höhenbereich passt.
            if (height > 80f)
            {
                height = 80f + 25f * (1f - Mathf.Exp(-(height - 80f) / 25f));
            }

            return height;
        }

        private static float SideRise(float distance)
        {
            return Mathf.Max(0f, distance) * 0.6f + Mathf.Max(0f, distance - 18f) * 0.9f;
        }

        // Abstand eines Punkts zu einem Rechteck (0, wenn er darin liegt).
        private static float DistanceOutside(float x, float z, float minX, float maxX, float minZ, float maxZ)
        {
            float dx = Mathf.Max(minX - x, 0f, x - maxX);
            float dz = Mathf.Max(minZ - z, 0f, z - maxZ);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // Höhe des Talstation-Absatzes: so hoch, wie der Hang in der Mitte der Station ohnehin wäre.
        private static float StationHeight => -(ShelfMinZ - ValleyStationZ) * ValleySlope;

        private static float StationDistance(float x, float z)
        {
            return DistanceOutside(x, z, ValleyStationX - StationHalfX, ValleyStationX + StationHalfX,
                ValleyStationZ - StationHalfZ, ValleyStationZ + StationHalfZ);
        }

        // Schneidet den Absatz in den Hang: Im Rechteck ist es eben, daneben weicht das Gelände höchstens
        // StationBankSlope je Meter Abstand davon ab. Bergseits wird so eine Böschung abgetragen, talseits
        // eine aufgeschüttet. Der natürliche Hang (0,45) ist flacher als die Böschung, deshalb greift
        // sie nur in den ersten gut 5 m und endet lange vor StationBankReach.
        private static float ApplyStationTerrace(float x, float z, float height)
        {
            float distance = StationDistance(x, z);
            if (distance >= StationBankReach)
            {
                return height;
            }

            float bank = distance * StationBankSlope;
            return Mathf.Clamp(height, StationHeight - bank, StationHeight + bank);
        }

        private static float[,] BuildHeights()
        {
            int resolution = HeightResolution;
            float step = SizeXZ / (resolution - 1);
            var meters = new float[resolution, resolution];
            for (int row = 0; row < resolution; row++)
            {
                for (int column = 0; column < resolution; column++)
                {
                    float x = OriginX + column * step;
                    float z = OriginZ + row * step;
                    meters[row, column] = ApplyStationTerrace(x, z, RawHeight(x, z));
                }
            }

            // Kanten zwischen Platz, Absatz und Hang etwas runden (rund 1,5 m).
            for (int pass = 0; pass < 4; pass++)
            {
                meters = Blur(meters);
            }

            var normalized = new float[resolution, resolution];
            for (int row = 0; row < resolution; row++)
            {
                for (int column = 0; column < resolution; column++)
                {
                    float x = OriginX + column * step;
                    float z = OriginZ + row * step;
                    float height = meters[row, column];

                    // Hüttenplatz exakt auf 0: Sockel, Schuppen und Bergstation stehen darauf.
                    if (DistanceOutside(x, z, ShelfMinX, ShelfMaxX, ShelfMinZ, ShelfMaxZ) <= 0f)
                    {
                        height = 0f;
                    }

                    // Absatz der Talstation exakt eben, damit die Laderampe nicht kippelt.
                    if (StationDistance(x, z) <= 0f)
                    {
                        height = StationHeight;
                    }

                    normalized[row, column] = Mathf.Clamp01((height - BaseY) / HeightRange);
                }
            }

            return normalized;
        }

        // Schreibt die steilste Stelle im Spielbereich ins Log (Spieler kommen Hänge bis 45 Grad hoch).
        private static void LogSteepestPlayArea(TerrainData data)
        {
            float steepest = 0f;
            float steepestX = 0f;
            float steepestZ = 0f;
            for (float z = PlayMinZ; z <= PlayMaxZ; z += 0.25f)
            {
                for (float x = PlayMinX; x <= PlayMaxX; x += 0.25f)
                {
                    float steepness = data.GetSteepness((x - OriginX) / SizeXZ, (z - OriginZ) / SizeXZ);
                    if (steepness > steepest)
                    {
                        steepest = steepness;
                        steepestX = x;
                        steepestZ = z;
                    }
                }
            }

            Debug.Log(LogPrefix + $"Steilste Stelle im Spielbereich: {steepest:0.0} Grad bei x {steepestX:0.0}, z {steepestZ:0.0}.");
        }

        private static float[,] Blur(float[,] source)
        {
            int rows = source.GetLength(0);
            int columns = source.GetLength(1);
            var result = new float[rows, columns];
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    float sum = 0f;
                    int count = 0;
                    for (int dr = -1; dr <= 1; dr++)
                    {
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            int r = row + dr;
                            int c = column + dc;
                            if (r >= 0 && r < rows && c >= 0 && c < columns)
                            {
                                sum += source[r, c];
                                count++;
                            }
                        }
                    }

                    result[row, column] = sum / count;
                }
            }

            return result;
        }

        // ----- Assets -----

        private static TerrainData SaveTerrainData(TerrainLayer snow, TerrainLayer rock)
        {
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            bool isNew = data == null;
            if (isNew)
            {
                data = new TerrainData();
            }

            // Auflösung vor der Größe setzen: Eine neue Auflösung skaliert die Größe mit.
            data.heightmapResolution = HeightResolution;
            data.size = new Vector3(SizeXZ, HeightRange, SizeXZ);
            data.SetHeights(0, 0, BuildHeights());

            data.alphamapResolution = AlphaResolution;
            data.terrainLayers = new[] { snow, rock };
            data.SetAlphamaps(0, 0, BuildSplat(data));

            if (isNew)
            {
                AssetDatabase.CreateAsset(data, TerrainDataPath);
            }
            else
            {
                EditorUtility.SetDirty(data);
            }

            return data;
        }

        // Fels, wo es steiler als etwa 35 Grad ist, sonst Schnee.
        private static float[,,] BuildSplat(TerrainData data)
        {
            int resolution = AlphaResolution;
            var splat = new float[resolution, resolution, 2];
            for (int row = 0; row < resolution; row++)
            {
                for (int column = 0; column < resolution; column++)
                {
                    float steepness = data.GetSteepness((column + 0.5f) / resolution, (row + 0.5f) / resolution);
                    float rockWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(30f, 40f, steepness));
                    splat[row, column, 0] = 1f - rockWeight;
                    splat[row, column, 1] = rockWeight;
                }
            }

            return splat;
        }

        // Kleine, kachelbare Textur: Grundfarbe mit feinem Korn und sanften Wellen.
        private static Texture2D SaveGrainTexture(string name, Color32 baseColor, int grain, int wave, int seed)
        {
            const int size = 128;
            string path = $"{TerrainFolder}/{name}.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            bool isNew = texture == null;
            if (isNew)
            {
                texture = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = name };
            }

            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;

            var random = new System.Random(seed);
            var pixels = new Color32[size * size];
            const float tau = Mathf.PI * 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Ganzzahlige Frequenzen, damit die Textur ohne Naht kachelt.
                    float waves = 0.5f * Mathf.Sin((x * 3 + y * 2) * tau / size) + 0.5f * Mathf.Sin((x - y * 4) * tau / size);
                    int offset = random.Next(-grain, grain + 1) + Mathf.RoundToInt(waves * wave);
                    pixels[y * size + x] = new Color32(Shift(baseColor.r, offset), Shift(baseColor.g, offset), Shift(baseColor.b, offset), 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true);

            if (isNew)
            {
                AssetDatabase.CreateAsset(texture, path);
            }
            else
            {
                EditorUtility.SetDirty(texture);
            }

            return texture;
        }

        private static byte Shift(byte value, int offset)
        {
            return (byte)Mathf.Clamp(value + offset, 0, 255);
        }

        private static TerrainLayer SaveLayer(string name, Texture2D texture, float tileSize, float smoothness)
        {
            string path = $"{TerrainFolder}/{name}.terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            bool isNew = layer == null;
            if (isNew)
            {
                layer = new TerrainLayer();
            }

            layer.diffuseTexture = texture;
            layer.tileSize = new Vector2(tileSize, tileSize);
            layer.smoothnessSource = TerrainLayerSmoothnessSource.ConstantOnly;
            layer.smoothness = smoothness;
            layer.metallic = 0f;

            if (isNew)
            {
                AssetDatabase.CreateAsset(layer, path);
            }
            else
            {
                EditorUtility.SetDirty(layer);
            }

            return layer;
        }

        private static PhysicsMaterial SaveSnowPhysics()
        {
            string path = $"{TerrainFolder}/Schnee Physik.asset";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            bool isNew = material == null;
            if (isNew)
            {
                material = new PhysicsMaterial("Schnee Physik");
            }

            material.staticFriction = SnowStaticFriction;
            material.dynamicFriction = SnowDynamicFriction;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
            material.bounciness = 0f;

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

        private static Material SaveTerrainMaterial(Shader terrainShader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
            if (material == null)
            {
                material = new Material(terrainShader);
                AssetDatabase.CreateAsset(material, TerrainMaterialPath);
            }

            return material;
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
    }
}
