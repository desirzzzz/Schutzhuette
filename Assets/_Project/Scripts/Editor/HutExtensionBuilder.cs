using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Schutzhuette.EditorTools
{
    /// <summary>
    /// Erweitert die Graubox nach der Skizze design/levels/huette-skizze.md: Schlafraum hinter der
    /// Stube, Seilbahn mit Berg- und Talstation, Generator-Schuppen, Hackstock und Suchgebiet.
    /// Die neuen Orte haben noch keine Funktion, sie sind Kulisse zum Ablaufen und Ausprobieren.
    /// </summary>
    /// <remarks>
    /// Aufruf über das Menü "Schutzhütte/Karte erweitern (Skizze)" oder im Batchmode mit
    /// -executeMethod Schutzhuette.EditorTools.HutExtensionBuilder.BuildFromCommandLine.
    /// Voraussetzung: Graubox-Hütte und "Farbe und Licht" sind schon gebaut.
    /// Gibt es den Berg (HutTerrainBuilder), stehen Talstation, Seil-Stütze, Hackstock und
    /// Suchgebiet auf dem Gelände; die Talstation liegt dann tiefer am Hang.
    /// Darf mehrmals laufen: Die Gruppe "Erweiterung" wird jedes Mal neu aufgebaut.
    /// Alles hier ist feste Kulisse ohne NetworkObject. Jeder Rechner lädt dieselbe Szene,
    /// deshalb muss davon nichts über das Netz. Die Maße stehen in docs/GRAUBOX.md.
    /// </remarks>
    public static class HutExtensionBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
        private const string HutRootName = "Huette";
        private const string ExtensionGroupName = "Erweiterung";
        private const string LogPrefix = "[Erweiterung] ";

        // Stütze an der Hangkante (z), damit das Seil nicht durch den Hüttenplatz schneidet.
        private const float SupportZ = -1.5f;

        /// <summary>Menüeintrag im Editor. Fragt vorher nach ungespeicherten Szenen.</summary>
        [MenuItem("Schutzhütte/Karte erweitern (Skizze)")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string error = Build(out int partCount);
            EditorUtility.DisplayDialog("Karte erweitern", error ?? $"Fertig: {partCount} Teile.", "OK");
        }

        /// <summary>Batchmode-Einstieg. Beendet Unity mit Code 1, wenn etwas schiefgeht.</summary>
        public static void BuildFromCommandLine()
        {
            string error = Build(out int partCount);
            if (error != null)
            {
                Debug.LogError(LogPrefix + error);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log(LogPrefix + $"Fertig: {partCount} Teile in {ScenePath}.");
        }

        /// <summary>
        /// Baut die Erweiterung in der Prototyp-Szene neu und speichert sie. Auch für HutTerrainBuilder.
        /// </summary>
        /// <returns>null, wenn alles geklappt hat, sonst eine Fehlermeldung.</returns>
        internal static string Build(out int partCount)
        {
            partCount = 0;
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                return "Shader \"Universal Render Pipeline/Lit\" nicht gefunden.";
            }

            // Neue Materialien vor dem Öffnen der Szene speichern: OpenScene entlädt nicht benutzte Assets.
            HutLookBuilder.SaveMaterial(lit, "Metall", new Color(0.3f, 0.32f, 0.35f), 0.45f);
            HutLookBuilder.SaveMaterial(lit, "Wolldecke", new Color(0.62f, 0.2f, 0.16f), 0.05f);
            HutLookBuilder.SaveMaterial(lit, "Tanne", new Color(0.13f, 0.27f, 0.2f), 0.1f);
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform hut = null;
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == HutRootName)
                {
                    hut = rootObject.transform;
                }
            }

            if (hut == null)
            {
                return $"In {ScenePath} gibt es kein \"{HutRootName}\". Erst die Graubox-Hütte bauen.";
            }

            Terrain terrain = FindTerrain(scene);
            string doorwayError = OpenDoorways(hut, ref partCount);
            if (doorwayError != null)
            {
                return doorwayError;
            }

            // Die Erweiterung jedes Mal komplett neu aufbauen, dann bleibt nichts Altes liegen.
            Transform oldExtension = hut.Find(ExtensionGroupName);
            if (oldExtension != null)
            {
                Object.DestroyImmediate(oldExtension.gameObject);
            }

            var extension = new GameObject(ExtensionGroupName).transform;
            extension.SetParent(hut, false);
            var groups = new Dictionary<string, Transform>();

            foreach (Part part in GetParts(terrain))
            {
                CreatePart(GetGroup(extension, groups, part.Group), part);
                partCount++;
            }

            // Das Seil läuft vom Rad der Bergstation über die Stütze an der Hangkante zum Rad der
            // Talstation (jeweils Unterkante Rad bzw. Oberkante Sattel).
            var topWheel = new Vector3(-11f, 3.2f, 15.3f);
            var saddle = new Vector3(-11f, GroundY(terrain, -11f, SupportZ) + 3.6f, SupportZ);
            Vector3 bottomWheel = GetValleyStationBase(terrain) + new Vector3(0.25f, 2.7f, 0.2f);
            Transform cableGroup = GetGroup(extension, groups, "Seil");
            CreateCable(cableGroup, "Tragseil oben", topWheel, saddle);
            CreateCable(cableGroup, "Tragseil Hang", saddle, bottomWheel);
            partCount += 2;

            HutLookBuilder.EnsureLamp(GetGroup(extension, groups, "Schlafraum"), "Lampe Schlafraum", new Vector3(2.5f, 3.95f, 23.6f), 2f, 5f);
            HutLookBuilder.EnsureLamp(GetGroup(extension, groups, "Bergstation"), "Lampe Bergstation", new Vector3(-11.25f, 3.85f, 17.5f), 2.5f, 6f);
            HutLookBuilder.EnsureLamp(GetGroup(extension, groups, "Generator-Schuppen"), "Lampe Schuppen", new Vector3(10.25f, 2.45f, 19f), 1.5f, 4f);
            partCount += 3;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                return $"Szene konnte nicht gespeichert werden: {ScenePath}";
            }

            return null;
        }

        // Neue Türen in die Rückwand (zum Schlafraum) und in die Westwand des Lagers (zur Bergstation).
        // Die alten durchgehenden Wände werden durch Stücke mit Türöffnung und Sturz ersetzt.
        private static string OpenDoorways(Transform hut, ref int partCount)
        {
            Transform outerWalls = hut.Find("Außenwände");
            Transform furniture = hut.Find("Einrichtung");
            if (outerWalls == null || furniture == null)
            {
                return "Gruppe \"Außenwände\" oder \"Einrichtung\" fehlt in der Hütte.";
            }

            RemoveChild(outerWalls, "Wand hinten");
            RemoveChild(outerWalls, "Wand links");

            const string wood = "Holz Aussen";
            var doorwayParts = new[]
            {
                // Rückwand bei z = 21.85: Tür zum Schlafraum von x 3.9 bis 5.3, rechts neben dem Ofen.
                Part.Box(null, "Wand hinten links", new Vector3(-1.05f, 2.9f, 21.85f), new Vector3(9.9f, 2.8f, 0.3f), wood),
                Part.Box(null, "Wand hinten rechts", new Vector3(5.65f, 2.9f, 21.85f), new Vector3(0.7f, 2.8f, 0.3f), wood),
                Part.Box(null, "Sturz Schlafraum", new Vector3(4.6f, 4.05f, 21.85f), new Vector3(1.4f, 0.5f, 0.3f), wood),

                // Westwand bei x = -5.85: Tür vom Lager zur Bergstation von z 16.3 bis 17.7.
                Part.Box(null, "Wand links vorne", new Vector3(-5.85f, 2.9f, 14.15f), new Vector3(0.3f, 2.8f, 4.3f), wood),
                Part.Box(null, "Wand links hinten", new Vector3(-5.85f, 2.9f, 19.85f), new Vector3(0.3f, 2.8f, 4.3f), wood),
                Part.Box(null, "Sturz Bergstation", new Vector3(-5.85f, 4.05f, 17f), new Vector3(0.3f, 0.5f, 1.4f), wood),
            };

            foreach (Part part in doorwayParts)
            {
                RemoveChild(outerWalls, part.Name);
                CreatePart(outerWalls, part);
                partCount++;
            }

            // Das Regal stand vor der neuen Tür. Es kommt an die Ostwand des Lagers (Innenwand zur Stube).
            Transform shelf = furniture.Find("Regal Lager");
            if (shelf != null)
            {
                shelf.position = new Vector3(-1.4f, 2.5f, 17f);
            }
            else
            {
                Debug.LogWarning(LogPrefix + "\"Regal Lager\" nicht gefunden, nicht verschoben.");
            }

            return null;
        }

        // Die Bautabelle der Erweiterung. Eine Methode statt eines statischen Feldes,
        // damit bei ausgeschaltetem Domain Reload nichts zwischen zwei Läufen hängen bleibt.
        private static List<Part> GetParts(Terrain terrain)
        {
            const string bedroom = "Schlafraum";
            const string top = "Bergstation";
            const string valley = "Talstation";
            const string shed = "Generator-Schuppen";
            const string woodYard = "Holzplatz";
            const string search = "Suchgebiet";
            const string cable = "Seil";

            Vector3 valleyBase = GetValleyStationBase(terrain);
            float supportGround = GroundY(terrain, -11f, SupportZ);

            var parts = new List<Part>
            {
                // ----- Schlafraum: x -1 bis 6, z 22 bis 25,5, Boden auf 1,5 m wie die Hütte -----
                // Der Sockel wird nach hinten verlängert, mit 1 m Rand wie beim Hauptsockel.
                Part.Box(bedroom, "Sockel Schlafraum", new Vector3(2.5f, 0.75f, 24.75f), new Vector3(9f, 1.5f, 3.5f), "Stein"),
                Part.Box(bedroom, "Wand West", new Vector3(-0.85f, 2.9f, 23.75f), new Vector3(0.3f, 2.8f, 3.5f), "Holz Aussen"),
                Part.Box(bedroom, "Wand Ost", new Vector3(5.85f, 2.9f, 23.75f), new Vector3(0.3f, 2.8f, 3.5f), "Holz Aussen"),
                Part.Box(bedroom, "Wand Nord", new Vector3(2.5f, 2.9f, 25.35f), new Vector3(7f, 2.8f, 0.3f), "Holz Aussen"),
                Part.Box(bedroom, "Dielenboden", new Vector3(2.5f, 1.51f, 23.6f), new Vector3(6.4f, 0.02f, 3.2f), "Dielen", solid: false),
                // Schließt an das Hauptdach an (dessen Überstand endet bei z 22,4).
                Part.Box(bedroom, "Dach", new Vector3(2.5f, 4.45f, 24.15f), new Vector3(7.8f, 0.3f, 3.5f), "Holz Innen"),

                // ----- Bergstation: Plattform auf Höhe des Hüttenbodens, direkt vor der neuen Lagertür -----
                Part.Box(top, "Plattform", new Vector3(-10.75f, 0.75f, 17f), new Vector3(7.5f, 1.5f, 4f), "Stein"),
                Part.Box(top, "Bretterboden", new Vector3(-10.75f, 1.51f, 17f), new Vector3(7.4f, 0.02f, 3.9f), "Dielen", solid: false),
                Part.Box(top, "Pfosten vorne links", new Vector3(-14.3f, 2.825f, 15.2f), new Vector3(0.2f, 2.65f, 0.2f), "Holz Aussen"),
                Part.Box(top, "Pfosten hinten links", new Vector3(-14.3f, 2.825f, 18.8f), new Vector3(0.2f, 2.65f, 0.2f), "Holz Aussen"),
                Part.Box(top, "Pfosten vorne rechts", new Vector3(-8.2f, 2.825f, 15.2f), new Vector3(0.2f, 2.65f, 0.2f), "Holz Aussen"),
                Part.Box(top, "Pfosten hinten rechts", new Vector3(-8.2f, 2.825f, 18.8f), new Vector3(0.2f, 2.65f, 0.2f), "Holz Aussen"),
                Part.Box(top, "Dach", new Vector3(-11.25f, 4.25f, 17f), new Vector3(7f, 0.2f, 4.4f), "Holz Innen"),
                // Geländer, damit niemand mit einer Kiste 1,5 m tief fällt. Vorne bleibt eine Lücke für das Seil.
                Part.Box(top, "Geländer West", new Vector3(-14.45f, 2f, 17f), new Vector3(0.06f, 1f, 4f), "Holz Aussen"),
                Part.Box(top, "Geländer Nord", new Vector3(-10.75f, 2f, 18.97f), new Vector3(7.5f, 1f, 0.06f), "Holz Aussen"),
                Part.Box(top, "Geländer Süd links", new Vector3(-13.25f, 2f, 15.03f), new Vector3(2.5f, 1f, 0.06f), "Holz Aussen"),
                Part.Box(top, "Geländer Süd rechts", new Vector3(-8.5f, 2f, 15.03f), new Vector3(3f, 1f, 0.06f), "Holz Aussen"),
                // Kurbel: Sockel, Rad und Griff. Kurbeln kommt mit der echten Seilbahn.
                Part.Box(top, "Kurbel Sockel", new Vector3(-12.8f, 2f, 15.8f), new Vector3(0.4f, 1f, 0.4f), "Metall"),
                Part.Shape(top, "Kurbel Rad", PrimitiveType.Cylinder, new Vector3(-12.55f, 2.4f, 15.8f), new Vector3(0f, 0f, 90f), new Vector3(0.7f, 0.05f, 0.7f), "Metall", solid: false),
                Part.Box(top, "Kurbel Griff", new Vector3(-12.42f, 2.65f, 15.8f), new Vector3(0.2f, 0.05f, 0.05f), "Metall", solid: false),
                // Seilrad unter dem Dach über der Lücke im Geländer, dazu die Ablage für ankommende Lasten.
                Part.Shape(top, "Seilrad", PrimitiveType.Cylinder, new Vector3(-11f, 3.6f, 15.3f), new Vector3(0f, 0f, 90f), new Vector3(0.8f, 0.05f, 0.8f), "Metall", solid: false),
                Part.Box(top, "Seilrad Halter", new Vector3(-11f, 3.875f, 15.3f), new Vector3(0.1f, 0.55f, 0.1f), "Metall", solid: false),
                Part.Box(top, "Ablage", new Vector3(-11f, 1.53f, 15.6f), new Vector3(1.6f, 0.03f, 1f), "Metall", solid: false),

                // ----- Talstation: flache Laderampe (0,25 m, eine Stufe) mit Gerüst, Lage relativ zur Rampenmitte -----
                Part.Box(valley, "Laderampe", valleyBase + new Vector3(0f, 0.125f, 0f), new Vector3(6.5f, 0.25f, 4f), "Dielen"),
                Part.Box(valley, "Gerüst links", valleyBase + new Vector3(-0.55f, 1.9f, 0.2f), new Vector3(0.2f, 3.3f, 0.2f), "Holz Aussen"),
                Part.Box(valley, "Gerüst rechts", valleyBase + new Vector3(1.05f, 1.9f, 0.2f), new Vector3(0.2f, 3.3f, 0.2f), "Holz Aussen"),
                Part.Box(valley, "Gerüst Balken", valleyBase + new Vector3(0.25f, 3.65f, 0.2f), new Vector3(1.8f, 0.2f, 0.2f), "Holz Aussen"),
                Part.Shape(valley, "Seilrad", PrimitiveType.Cylinder, valleyBase + new Vector3(0.25f, 3.1f, 0.2f), new Vector3(0f, 0f, 90f), new Vector3(0.8f, 0.05f, 0.8f), "Metall", solid: false),
                Part.Box(valley, "Ablage", valleyBase + new Vector3(0.25f, 0.27f, 0.2f), new Vector3(1.2f, 0.03f, 1f), "Metall", solid: false),

                // ----- Stütze an der Hangkante: hält das Seil über dem Hüttenplatz in gut 3 m Höhe -----
                Part.Box(cable, "Stütze Mast", new Vector3(-11f, supportGround + 1.75f, SupportZ), new Vector3(0.25f, 3.5f, 0.25f), "Holz Aussen"),
                Part.Box(cable, "Stütze Sattel", new Vector3(-11f, supportGround + 3.55f, SupportZ), new Vector3(0.2f, 0.1f, 0.8f), "Metall", solid: false),

                // ----- Generator-Schuppen: x 7,5 bis 13, z 17 bis 21, auf dem Boden, Tür nach vorn -----
                Part.Box(shed, "Bretterboden", new Vector3(10.25f, 0.01f, 19f), new Vector3(5.1f, 0.02f, 3.6f), "Dielen", solid: false),
                Part.Box(shed, "Wand Nord", new Vector3(10.25f, 1.35f, 20.9f), new Vector3(5.5f, 2.7f, 0.2f), "Holz Aussen"),
                Part.Box(shed, "Wand West", new Vector3(7.6f, 1.35f, 19f), new Vector3(0.2f, 2.7f, 3.6f), "Holz Aussen"),
                Part.Box(shed, "Wand Ost", new Vector3(12.9f, 1.35f, 19f), new Vector3(0.2f, 2.7f, 3.6f), "Holz Aussen"),
                Part.Box(shed, "Wand Süd links", new Vector3(8.525f, 1.35f, 17.1f), new Vector3(2.05f, 2.7f, 0.2f), "Holz Aussen"),
                Part.Box(shed, "Wand Süd rechts", new Vector3(11.975f, 1.35f, 17.1f), new Vector3(2.05f, 2.7f, 0.2f), "Holz Aussen"),
                Part.Box(shed, "Sturz", new Vector3(10.25f, 2.5f, 17.1f), new Vector3(1.4f, 0.4f, 0.2f), "Holz Aussen"),
                Part.Box(shed, "Dach", new Vector3(10.25f, 2.8f, 19f), new Vector3(5.9f, 0.2f, 4.4f), "Holz Innen"),
                Part.Box(shed, "Generator", new Vector3(10.25f, 0.45f, 20.2f), new Vector3(1.3f, 0.9f, 0.8f), "Metall"),
                Part.Shape(shed, "Auspuff", PrimitiveType.Cylinder, new Vector3(10.75f, 1.2f, 20.4f), Vector3.zero, new Vector3(0.12f, 0.3f, 0.12f), "Metall", solid: false),

                // ----- Hackstock rechts neben dem Holz (die Truhe steht dort, wo die Skizze ihn hatte) -----
                Part.Shape(woodYard, "Hackstock", PrimitiveType.Cylinder, new Vector3(7.5f, GroundY(terrain, 7.5f, 2.3f) + 0.25f, 2.3f), Vector3.zero, new Vector3(0.6f, 0.25f, 0.6f), "Moebelholz"),
            };

            parts.AddRange(GetBeds());

            // ----- Suchgebiet: x 4 bis 16, z -11 bis -1, Felsen halb im Schnee -----
            AddRock(parts, terrain, search, 1, new Vector3(6f, 0.4f, -3f), new Vector3(5f, 20f, 3f), new Vector3(1.6f, 1f, 1.2f));
            AddRock(parts, terrain, search, 2, new Vector3(9.5f, 0.55f, -2.2f), new Vector3(-4f, -15f, 6f), new Vector3(2.2f, 1.4f, 1.6f));
            AddRock(parts, terrain, search, 3, new Vector3(13.5f, 0.3f, -4f), new Vector3(8f, 40f, -5f), new Vector3(1.2f, 0.8f, 1f));
            AddRock(parts, terrain, search, 4, new Vector3(7.5f, 0.75f, -7.5f), new Vector3(-6f, 10f, 4f), new Vector3(2.6f, 1.8f, 2f));
            AddRock(parts, terrain, search, 5, new Vector3(11.5f, 0.4f, -9f), new Vector3(5f, -30f, -7f), new Vector3(1.4f, 1f, 1.4f));
            AddRock(parts, terrain, search, 6, new Vector3(15f, 0.65f, -8f), new Vector3(-3f, 25f, 8f), new Vector3(1.8f, 1.6f, 1.4f));
            AddRock(parts, terrain, search, 7, new Vector3(5f, 0.25f, -10f), new Vector3(6f, 0f, -4f), new Vector3(1f, 0.6f, 0.8f));

            // Tannen: Stamm, breite Krone, schmale Spitze. Gut zur Orientierung in der Nacht.
            AddFir(parts, terrain, search, 1, new Vector2(10.5f, -5.5f));
            AddFir(parts, terrain, search, 2, new Vector2(14f, -1.8f));
            AddFir(parts, terrain, search, 3, new Vector2(5.2f, -5.6f));
            AddFir(parts, terrain, search, 4, new Vector2(12.5f, -10.5f));
            AddFir(parts, terrain, search, 5, new Vector2(8.5f, -10f));

            return parts;
        }

        // position.y ist die Höhe der Felsmitte über dem Boden. Am Hang etwas tiefer eingegraben,
        // damit auf der Talseite keine Lücke unter dem Fels bleibt.
        private static void AddRock(List<Part> parts, Terrain terrain, string group, int number, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            float bury = terrain != null ? 0.15f : 0f;
            position.y += GroundY(terrain, position.x, position.z) - bury;
            parts.Add(Part.Shape(group, $"Fels {number}", PrimitiveType.Cube, position, rotation, scale, "Stein"));
        }

        private static void AddFir(List<Part> parts, Terrain terrain, string group, int number, Vector2 groundPosition)
        {
            float x = groundPosition.x;
            float z = groundPosition.y;
            float ground = GroundY(terrain, x, z) - (terrain != null ? 0.1f : 0f);
            parts.Add(Part.Shape(group, $"Tanne {number} Stamm", PrimitiveType.Cylinder, new Vector3(x, ground + 0.6f, z), Vector3.zero, new Vector3(0.25f, 0.6f, 0.25f), "Moebelholz"));
            parts.Add(Part.Shape(group, $"Tanne {number} Krone", PrimitiveType.Capsule, new Vector3(x, ground + 2.6f, z), Vector3.zero, new Vector3(1.4f, 1.4f, 1.4f), "Tanne"));
            parts.Add(Part.Shape(group, $"Tanne {number} Spitze", PrimitiveType.Capsule, new Vector3(x, ground + 4.1f, z), Vector3.zero, new Vector3(0.8f, 0.8f, 0.8f), "Tanne", solid: false));
        }

        // Bodenhöhe an einer Stelle: das Gelände, falls es eins gibt, sonst die alte Bodenplatte auf 0.
        private static float GroundY(Terrain terrain, float x, float z)
        {
            return terrain == null ? 0f : terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
        }

        // Mitte der Laderampe auf Bodenhöhe: ohne Gelände wie in der Skizze, mit Gelände tiefer am Hang.
        private static Vector3 GetValleyStationBase(Terrain terrain)
        {
            if (terrain == null)
            {
                return new Vector3(-11.25f, 0f, -6f);
            }

            const float x = HutTerrainBuilder.ValleyStationX;
            const float z = HutTerrainBuilder.ValleyStationZ;
            return new Vector3(x, GroundY(terrain, x, z), z);
        }

        private static Terrain FindTerrain(Scene scene)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.TryGetComponent(out Terrain terrain))
                {
                    return terrain;
                }
            }

            return null;
        }

        // Vier Betten an der Nordwand: Gestell mit Collider, Decke und Kissen nur zum Ansehen.
        private static IEnumerable<Part> GetBeds()
        {
            float[] bedX = { -0.15f, 1.05f, 2.25f, 3.45f };
            for (int i = 0; i < bedX.Length; i++)
            {
                float x = bedX[i];
                yield return Part.Box("Schlafraum", $"Bett {i + 1}", new Vector3(x, 1.725f, 24.2f), new Vector3(0.9f, 0.45f, 2f), "Moebelholz");
                yield return Part.Box("Schlafraum", $"Bett {i + 1} Decke", new Vector3(x, 1.99f, 24f), new Vector3(0.86f, 0.08f, 1.5f), "Wolldecke", solid: false);
                yield return Part.Box("Schlafraum", $"Bett {i + 1} Kissen", new Vector3(x, 2f, 24.95f), new Vector3(0.6f, 0.1f, 0.35f), "Schnee", solid: false);
            }
        }

        private static Transform GetGroup(Transform extension, Dictionary<string, Transform> groups, string name)
        {
            if (!groups.TryGetValue(name, out Transform group))
            {
                group = new GameObject(name).transform;
                group.SetParent(extension, false);
                groups.Add(name, group);
            }

            return group;
        }

        private static GameObject CreatePart(Transform parent, Part part)
        {
            GameObject shape = GameObject.CreatePrimitive(part.Primitive);
            shape.name = part.Name;
            shape.transform.SetParent(parent, false);
            shape.transform.SetPositionAndRotation(part.Position, Quaternion.Euler(part.Rotation));
            shape.transform.localScale = part.Scale;

            if (!part.Solid && shape.TryGetComponent(out Collider shapeCollider))
            {
                Object.DestroyImmediate(shapeCollider);
            }

            Material material = HutLookBuilder.LoadMaterial(part.Material);
            if (material != null)
            {
                shape.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
            else
            {
                Debug.LogWarning(LogPrefix + $"Material \"{part.Material}\" fehlt für {part.Name}. Erst \"Farbe und Licht setzen\" laufen lassen.");
            }

            return shape;
        }

        // Ein dünner, langer Quader zwischen zwei Punkten. Ohne Collider, damit niemand am Seil hängen bleibt.
        private static void CreateCable(Transform parent, string name, Vector3 top, Vector3 bottom)
        {
            Vector3 span = top - bottom;
            GameObject cable = CreatePart(parent, Part.Box(null, name, (top + bottom) * 0.5f, new Vector3(0.04f, 0.04f, span.magnitude), "Metall", solid: false));
            cable.transform.rotation = Quaternion.LookRotation(span);
            cable.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void RemoveChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        // Ein Teil der Bautabelle: Gruppe, Name, Grundform, Weltposition, Drehung in Grad,
        // Größe (bei Würfeln in Metern), Material und ob man dagegen laufen kann (Collider).
        private readonly struct Part
        {
            public readonly string Group;
            public readonly string Name;
            public readonly PrimitiveType Primitive;
            public readonly Vector3 Position;
            public readonly Vector3 Rotation;
            public readonly Vector3 Scale;
            public readonly string Material;
            public readonly bool Solid;

            private Part(string group, string name, PrimitiveType shape, Vector3 position, Vector3 rotation, Vector3 scale, string material, bool solid)
            {
                Group = group;
                Name = name;
                Primitive = shape;
                Position = position;
                Rotation = rotation;
                Scale = scale;
                Material = material;
                Solid = solid;
            }

            public static Part Box(string group, string name, Vector3 position, Vector3 scale, string material, bool solid = true)
            {
                return new Part(group, name, PrimitiveType.Cube, position, Vector3.zero, scale, material, solid);
            }

            public static Part Shape(string group, string name, PrimitiveType shape, Vector3 position, Vector3 rotation, Vector3 scale, string material, bool solid = true)
            {
                return new Part(group, name, shape, position, rotation, scale, material, solid);
            }
        }
    }
}
