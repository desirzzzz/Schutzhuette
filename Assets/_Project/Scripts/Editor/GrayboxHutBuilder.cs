using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Schutzhuette.EditorTools
{
    /// <summary>
    /// Legt den Rohbau der Graubox-Hütte aus docs/GRAUBOX.md in der Prototyp-Szene an:
    /// Sockel, Hang, Wände mit Türöffnungen und Platzhalter-Möbel, alles aus Würfeln.
    /// </summary>
    /// <remarks>
    /// Aufruf über das Menü "Schutzhütte/Graubox-Hütte bauen" oder im Batchmode mit
    /// -executeMethod Schutzhuette.EditorTools.GrayboxHutBuilder.BuildFromCommandLine.
    /// Überschreibt nichts: Gibt es in der Szene schon ein Objekt "Huette", bricht es ab.
    /// Die Maße stehen in docs/GRAUBOX.md. Wer dort etwas ändert, ändert es hier mit.
    /// </remarks>
    public static class GrayboxHutBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
        private const string RootName = "Huette";
        private const string LogPrefix = "[Graubox] ";

        /// <summary>Menüeintrag im Editor. Fragt vorher nach ungespeicherten Szenen.</summary>
        [MenuItem("Schutzhütte/Graubox-Hütte bauen")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string error = Build(out int partCount);
            EditorUtility.DisplayDialog("Graubox-Hütte", error ?? $"Fertig: {partCount} Teile.", "OK");
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

        // Gibt null zurück, wenn alles geklappt hat, sonst eine Fehlermeldung.
        private static string Build(out int partCount)
        {
            partCount = 0;
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject ground = null;
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == RootName)
                {
                    return $"In {ScenePath} gibt es schon \"{RootName}\". Nichts geändert.";
                }

                if (rootObject.name == "Ground")
                {
                    ground = rootObject;
                }
            }

            // Boden vergrößern: Eine Unity-Plane ist 10 x 10 m, Scale 6 ergibt 60 x 60 m.
            if (ground != null)
            {
                ground.transform.localScale = new Vector3(6f, 1f, 6f);
            }
            else
            {
                Debug.LogWarning(LogPrefix + "Kein Objekt \"Ground\" gefunden, Boden nicht vergrößert.");
            }

            var hut = new GameObject(RootName).transform;
            var groups = new Dictionary<string, Transform>();

            foreach (Part part in GetParts())
            {
                if (!groups.TryGetValue(part.Group, out Transform group))
                {
                    group = new GameObject(part.Group).transform;
                    group.SetParent(hut, false);
                    groups.Add(part.Group, group);
                }

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = part.Name;
                cube.transform.SetParent(group, false);
                cube.transform.SetPositionAndRotation(part.Position, Quaternion.Euler(part.Rotation));
                cube.transform.localScale = part.Scale;
                partCount++;
            }

            if (!EditorSceneManager.SaveScene(scene))
            {
                return $"Szene konnte nicht gespeichert werden: {ScenePath}";
            }

            return null;
        }

        // Die Bautabelle aus docs/GRAUBOX.md. Eine Methode statt eines statischen Feldes,
        // damit bei ausgeschaltetem Domain Reload nichts zwischen zwei Läufen hängen bleibt.
        private static Part[] GetParts()
        {
            const string terrain = "Gelände";
            const string outer = "Außenwände";
            const string inner = "Innenwände";
            const string furniture = "Einrichtung";

            return new[]
            {
                // Hüttenboden = Oberkante Sockel auf 1,5 m. Der Hang ist ein gekippter Quader,
                // seine Oberkante läuft von (z 2, Höhe 0) bis (z 10, Höhe 1,5).
                new Part(terrain, "Sockel", new Vector3(0f, 0.75f, 16.5f), new Vector3(14f, 1.5f, 13f)),
                new Part(terrain, "Hang", new Vector3(-2f, 0.5f, 6.05f), new Vector3(-10.6f, 0f, 0f), new Vector3(10f, 0.5f, 8.15f)),

                // Außenwände: 0,3 m dick, 2,8 m hoch. Türen sind 2,3 m hoch, darüber ein Sturz.
                new Part(outer, "Wand vorne links", new Vector3(-5.15f, 2.9f, 12.15f), new Vector3(1.7f, 2.8f, 0.3f)),
                new Part(outer, "Wand vorne rechts", new Vector3(1.65f, 2.9f, 12.15f), new Vector3(8.7f, 2.8f, 0.3f)),
                new Part(outer, "Sturz Eingang", new Vector3(-3.5f, 4.05f, 12.15f), new Vector3(1.6f, 0.5f, 0.3f)),
                new Part(outer, "Wand hinten", new Vector3(0f, 2.9f, 21.85f), new Vector3(12f, 2.8f, 0.3f)),
                new Part(outer, "Wand links", new Vector3(-5.85f, 2.9f, 17f), new Vector3(0.3f, 2.8f, 10f)),
                new Part(outer, "Wand rechts", new Vector3(5.85f, 2.9f, 17f), new Vector3(0.3f, 2.8f, 10f)),

                // Innenwand zur Stube bei x = -1, mit Türen zu Vorraum und Küche.
                new Part(inner, "Innenwand Stube vorne", new Vector3(-1f, 2.9f, 12.65f), new Vector3(0.2f, 2.8f, 0.7f)),
                new Part(inner, "Innenwand Stube Mitte", new Vector3(-1f, 2.9f, 16.85f), new Vector3(0.2f, 2.8f, 4.9f)),
                new Part(inner, "Innenwand Stube hinten", new Vector3(-1f, 2.9f, 21.2f), new Vector3(0.2f, 2.8f, 1f)),
                new Part(inner, "Sturz Vorraum-Stube", new Vector3(-1f, 4.05f, 13.7f), new Vector3(0.2f, 0.5f, 1.4f)),
                new Part(inner, "Sturz Küche-Stube", new Vector3(-1f, 4.05f, 20f), new Vector3(0.2f, 0.5f, 1.4f)),

                // Wand Vorraum / Lager bei z = 15.5, Tür 1,4 m.
                new Part(inner, "Wand Vorraum-Lager links", new Vector3(-4.95f, 2.9f, 15.5f), new Vector3(1.5f, 2.8f, 0.2f)),
                new Part(inner, "Wand Vorraum-Lager rechts", new Vector3(-1.95f, 2.9f, 15.5f), new Vector3(1.7f, 2.8f, 0.2f)),
                new Part(inner, "Sturz Vorraum-Lager", new Vector3(-3.5f, 4.05f, 15.5f), new Vector3(1.4f, 0.5f, 0.2f)),

                // Wand Lager / Küche bei z = 18.5, absichtlich enge Tür 1,2 m.
                new Part(inner, "Wand Lager-Küche links", new Vector3(-4.9f, 2.9f, 18.5f), new Vector3(1.6f, 2.8f, 0.2f)),
                new Part(inner, "Wand Lager-Küche rechts", new Vector3(-2f, 2.9f, 18.5f), new Vector3(1.8f, 2.8f, 0.2f)),
                new Part(inner, "Sturz Lager-Küche", new Vector3(-3.5f, 4.05f, 18.5f), new Vector3(1.2f, 0.5f, 0.2f)),

                // Platzhalter-Möbel. Verschieben ist ausdrücklich erlaubt.
                new Part(furniture, "Ofen", new Vector3(2.2f, 2.1f, 20.9f), new Vector3(1.4f, 1.2f, 1.4f)),
                new Part(furniture, "Tisch 1", new Vector3(4f, 1.9f, 14.5f), new Vector3(1f, 0.8f, 2.4f)),
                new Part(furniture, "Bank 1 links", new Vector3(2.9f, 1.725f, 14.5f), new Vector3(0.4f, 0.45f, 2.4f)),
                new Part(furniture, "Bank 1 rechts", new Vector3(5.1f, 1.725f, 14.5f), new Vector3(0.4f, 0.45f, 2.4f)),
                new Part(furniture, "Tisch 2", new Vector3(4f, 1.9f, 18f), new Vector3(1f, 0.8f, 2.4f)),
                new Part(furniture, "Bank 2 links", new Vector3(2.9f, 1.725f, 18f), new Vector3(0.4f, 0.45f, 2.4f)),
                new Part(furniture, "Bank 2 rechts", new Vector3(5.1f, 1.725f, 18f), new Vector3(0.4f, 0.45f, 2.4f)),
                new Part(furniture, "Tisch 3", new Vector3(1f, 1.9f, 16.5f), new Vector3(1.2f, 0.8f, 1.2f)),
                new Part(furniture, "Regal Lager", new Vector3(-5.3f, 2.5f, 17f), new Vector3(0.6f, 2f, 2.4f)),
                new Part(furniture, "Küchenzeile", new Vector3(-3.4f, 1.95f, 21.3f), new Vector3(3f, 0.9f, 0.7f)),
            };
        }

        // Ein Quader aus der Bautabelle: Gruppe, Name, Weltposition, Drehung in Grad, Größe in Metern.
        private readonly struct Part
        {
            public readonly string Group;
            public readonly string Name;
            public readonly Vector3 Position;
            public readonly Vector3 Rotation;
            public readonly Vector3 Scale;

            public Part(string group, string name, Vector3 position, Vector3 scale)
                : this(group, name, position, Vector3.zero, scale)
            {
            }

            public Part(string group, string name, Vector3 position, Vector3 rotation, Vector3 scale)
            {
                Group = group;
                Name = name;
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }
        }
    }
}
