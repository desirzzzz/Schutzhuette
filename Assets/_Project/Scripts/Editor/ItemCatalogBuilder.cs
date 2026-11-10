using System;
using System.Collections.Generic;
using Schutzhuette.Audio;
using Schutzhuette.Carrying;
using Schutzhuette.Items;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Schutzhuette.EditorTools
{
    /// <summary>
    /// Legt den Startkatalog an: Gegenstandsarten, Waren, den Katalog und fehlende Prefabs
    /// (GDD Gegenstandsarten, Tabelle "Startkatalog für den Vertical Slice"; ADR-0002).
    /// Stand Teil 1: Vorratskiste, Holzscheit, Truhe und Tasse Tee, dazu alle sieben Waren.
    /// </summary>
    /// <remarks>
    /// Aufruf über das Menü "Schutzhütte/Katalog anlegen" oder im Batchmode in zwei Starts:
    /// zuerst -executeMethod Schutzhuette.EditorTools.ItemCatalogBuilder.BuildFromCommandLine,
    /// danach -executeMethod Schutzhuette.EditorTools.ItemCatalogBuilder.VerifyFromCommandLine.
    /// Der zweite Start ist nötig, weil NGO die Netzwerk-ID eines neuen Prefabs erst beim Laden ausrechnet.
    /// Das Skript legt nur an, was fehlt. Vorhandene Arten, Waren und Prefabs bleiben, wie sie sind;
    /// dem Katalog werden nur fehlende Einträge angehängt.
    /// </remarks>
    public static class ItemCatalogBuilder
    {
        private const string DataFolder = "Assets/_Project/Data";
        private const string ItemsFolderName = "Items";
        private const string WaresFolderName = "Wares";
        private const string ItemsFolder = DataFolder + "/" + ItemsFolderName;
        private const string WaresFolder = DataFolder + "/" + WaresFolderName;
        private const string CatalogPath = ItemsFolder + "/ItemCatalog.asset";

        private const string NetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";
        private const string SoundSettingsPath = DataFolder + "/SoundSettings.asset";

        private const string CratePrefabPath = "Assets/_Project/Prefabs/Crate.prefab";
        private const string FirewoodPrefabPath = "Assets/_Project/Prefabs/Firewood.prefab";
        private const string ChestPrefabPath = "Assets/_Project/Prefabs/HeavyChest.prefab";
        private const string TeaPrefabPath = "Assets/_Project/Prefabs/Tea.prefab";

        private const string PorcelainMaterialName = "Porzellan";
        private const string LogPrefix = "[Katalog] ";

        /// <summary>Menüeintrag im Editor.</summary>
        [MenuItem("Schutzhütte/Katalog anlegen")]
        public static void BuildFromMenu()
        {
            string error = Build() ?? Verify();
            EditorUtility.DisplayDialog("Katalog", error ?? "Fertig. Der Katalog hat keine Fehler.", "OK");
        }

        /// <summary>Batchmode-Einstieg, erster Start. Beendet Unity mit Code 1, wenn etwas schiefgeht.</summary>
        public static void BuildFromCommandLine()
        {
            string error = Build();
            if (error != null)
            {
                Debug.LogError(LogPrefix + error);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log(LogPrefix + "Angelegt. Jetzt VerifyFromCommandLine in einem eigenen Unity-Start ausführen.");
        }

        /// <summary>
        /// Batchmode-Einstieg, zweiter Start: Netzwerk-ID der neuen Prefabs, Eintrag in der
        /// Netzwerk-Prefab-Liste und Katalogprüfung. Beendet Unity mit Code 1 bei einem Fehler.
        /// </summary>
        public static void VerifyFromCommandLine()
        {
            string error = Verify();
            if (error != null)
            {
                Debug.LogError(LogPrefix + error);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log(LogPrefix + "Geprüft: Der Katalog hat keine Fehler.");
        }

        // Gibt null zurück, wenn alles geklappt hat, sonst eine Fehlermeldung.
        private static string Build()
        {
            EnsureFolder(DataFolder, ItemsFolderName);
            EnsureFolder(DataFolder, WaresFolderName);

            string error = null;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(TeaPrefabPath) == null)
            {
                error = CreateTeaPrefab();
            }

            var wares = new List<WareKind>();
            var kinds = new List<ItemKind>();
            error ??= BuildWares(wares);
            error ??= BuildKinds(kinds);
            error ??= BuildCatalog(kinds, wares);
            if (error != null)
            {
                return error;
            }

            AssetDatabase.SaveAssets();
            return null;
        }

        // Die sieben Waren aus dem GDD (vorläufig, das Kochen-Dokument legt sie fest).
        private static string BuildWares(List<WareKind> wares)
        {
            string error = null;
            error ??= AddWare(wares, "WareSpeck", "ware_speck", "Speck");
            error ??= AddWare(wares, "WareBrot", "ware_brot", "Brot");
            error ??= AddWare(wares, "WareMehl", "ware_mehl", "Mehl");
            error ??= AddWare(wares, "WareEier", "ware_eier", "Eier");
            error ??= AddWare(wares, "WareMilch", "ware_milch", "Milch");
            error ??= AddWare(wares, "WareTee", "ware_tee", "Tee");
            error ??= AddWare(wares, "WareWasser", "ware_wasser", "Wasser");
            return error;
        }

        // Werte aus der Tabelle "Startkatalog für den Vertical Slice" im GDD Gegenstandsarten.
        private static string BuildKinds(List<ItemKind> kinds)
        {
            string error = null;

            error ??= AddKind(kinds, "Vorratskiste", "vorratskiste", "Vorratskiste", 10f, CarryMode.Solo,
                ItemCategory.SupplyPack, ImpactSoundGroup.Wood, CratePrefabPath,
                kind => kind.EditorSetSupplyPack(new SupplyPackBlock(true, 6, null)));

            error ??= AddKind(kinds, "Holzscheit", "holzscheit", "Holzscheit", 3f, CarryMode.Solo,
                ItemCategory.Fuel, ImpactSoundGroup.Wood, FirewoodPrefabPath,
                kind => kind.EditorSetFuel(new FuelBlock(true, 45f)));

            error ??= AddKind(kinds, "Truhe", "truhe", "Vorratstruhe", 80f, CarryMode.Pair,
                ItemCategory.SupplyPack, ImpactSoundGroup.Wood, ChestPrefabPath,
                kind => kind.EditorSetSupplyPack(new SupplyPackBlock(true, 20, null)));

            // Das Bedürfnis bleibt leer, bis das Gäste-Dokument die Bedürfnisse festlegt.
            error ??= AddKind(kinds, "Tee", "tee", "Tasse Tee", 0.5f, CarryMode.Solo,
                ItemCategory.Dish, ImpactSoundGroup.Soft, TeaPrefabPath,
                kind =>
                {
                    kind.EditorSetDish(new DishBlock(true, null, 90f));
                    kind.EditorSetFragile(new FragileBlock(true, 7.0f));
                });

            return error;
        }

        private static string AddWare(List<WareKind> wares, string fileName, string id, string displayName)
        {
            string path = $"{WaresFolder}/{fileName}.asset";
            var ware = AssetDatabase.LoadAssetAtPath<WareKind>(path);
            if (ware == null)
            {
                ware = ScriptableObject.CreateInstance<WareKind>();
                ware.EditorSet(id, displayName);
                AssetDatabase.CreateAsset(ware, path);
                Debug.Log(LogPrefix + $"Ware angelegt: {path}");
            }

            wares.Add(ware);
            return null;
        }

        private static string AddKind(List<ItemKind> kinds, string fileName, string id, string displayName,
            float massKg, CarryMode carryMode, ItemCategory category, ImpactSoundGroup impactSound,
            string prefabPath, Action<ItemKind> setProperties)
        {
            string path = $"{ItemsFolder}/{fileName}.asset";
            var kind = AssetDatabase.LoadAssetAtPath<ItemKind>(path);
            if (kind == null)
            {
                var prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                NetworkObject prefab = prefabObject != null ? prefabObject.GetComponent<NetworkObject>() : null;
                if (prefab == null)
                {
                    return $"Prefab mit NetworkObject fehlt: {prefabPath}";
                }

                kind = ScriptableObject.CreateInstance<ItemKind>();
                kind.EditorSetBasics(id, displayName, massKg, carryMode, category, impactSound, prefab);
                setProperties(kind);
                AssetDatabase.CreateAsset(kind, path);
                Debug.Log(LogPrefix + $"Art angelegt: {path}");
            }

            kinds.Add(kind);
            return null;
        }

        // Legt den Katalog an oder hängt fehlende Einträge an. Vorhandene Einträge und ihre Reihenfolge bleiben.
        private static string BuildCatalog(List<ItemKind> kinds, List<WareKind> wares)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            bool isNew = catalog == null;
            if (isNew)
            {
                catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            }

            var allKinds = new List<ItemKind>(catalog.Kinds);
            var allWares = new List<WareKind>(catalog.Wares);
            int added = AppendMissing(allKinds, kinds) + AppendMissing(allWares, wares);

            if (isNew)
            {
                catalog.EditorSetContents(allKinds, allWares, catalog.Needs);
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                Debug.Log(LogPrefix + $"Katalog angelegt: {CatalogPath}");
            }
            else if (added > 0)
            {
                catalog.EditorSetContents(allKinds, allWares, catalog.Needs);
                EditorUtility.SetDirty(catalog);
                Debug.Log(LogPrefix + $"Katalog ergänzt um {added} Einträge.");
            }

            return null;
        }

        private static int AppendMissing<T>(List<T> target, List<T> wanted) where T : Object
        {
            int added = 0;
            foreach (T entry in wanted)
            {
                if (!target.Contains(entry))
                {
                    target.Add(entry);
                    added++;
                }
            }

            return added;
        }

        // Tasse Tee als Graubox: heller Zylinder, 12 cm hoch und breit, 0,5 kg.
        // Netz- und Physik-Bausteine wie bei der Kiste (CarrySetupBuilder).
        private static string CreateTeaPrefab()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                return "Shader \"Universal Render Pipeline/Lit\" nicht gefunden.";
            }

            var soundSettings = AssetDatabase.LoadAssetAtPath<SoundSettings>(SoundSettingsPath);
            if (soundSettings == null)
            {
                return $"{SoundSettingsPath} fehlt. Erst die Geräusche einrichten.";
            }

            Material porcelain = HutLookBuilder.LoadMaterial(PorcelainMaterialName)
                ?? HutLookBuilder.SaveMaterial(lit, PorcelainMaterialName, new Color(0.92f, 0.9f, 0.84f), 0.6f);

            // Der Zylinder von Unity ist 1 m breit und 2 m hoch.
            GameObject tea = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tea.name = "Tea";
            tea.transform.localScale = new Vector3(0.12f, 0.06f, 0.12f);
            tea.GetComponent<MeshRenderer>().sharedMaterial = porcelain;

            // Kasten statt Kapsel als Collider: Eine Tasse soll stehen bleiben und nicht wegrollen.
            Object.DestroyImmediate(tea.GetComponent<CapsuleCollider>());
            var box = tea.AddComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(1f, 2f, 1f);

            var body = tea.AddComponent<Rigidbody>();
            body.mass = 0.5f;

            tea.AddComponent<NetworkObject>();

            var networkTransform = tea.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            networkTransform.SyncScaleX = false;
            networkTransform.SyncScaleY = false;
            networkTransform.SyncScaleZ = false;
            networkTransform.SwitchTransformSpaceWhenParented = true;

            tea.AddComponent<NetworkRigidbody>();
            tea.AddComponent<Carryable>();

            var impactSound = tea.AddComponent<ImpactSound>();
            var serializedSound = new SerializedObject(impactSound);
            serializedSound.FindProperty("_settings").objectReferenceValue = soundSettings;
            serializedSound.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(tea, TeaPrefabPath, out bool saved);
            Object.DestroyImmediate(tea);
            if (!saved)
            {
                return $"Tassen-Prefab konnte nicht gespeichert werden: {TeaPrefabPath}";
            }

            Debug.Log(LogPrefix + $"Prefab angelegt: {TeaPrefabPath}");
            return null;
        }

        // Netzwerk-ID des Tassen-Prefabs, Eintrag in der Netzwerk-Prefab-Liste, Katalogprüfung.
        private static string Verify()
        {
            uint hash = PrototypeSceneBuilder.EnsureNetworkPrefabHash(TeaPrefabPath);
            if (hash == 0)
            {
                return $"GlobalObjectIdHash von {TeaPrefabPath} ist 0. VerifyFromCommandLine in einem "
                    + "eigenen Unity-Start nach BuildFromCommandLine ausführen.";
            }

            Debug.Log(LogPrefix + $"GlobalObjectIdHash von {TeaPrefabPath}: {hash}");

            var networkPrefabs = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NetworkPrefabsPath);
            if (networkPrefabs == null)
            {
                return $"Netzwerk-Prefab-Liste fehlt: {NetworkPrefabsPath}";
            }

            var teaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TeaPrefabPath);
            if (networkPrefabs.Contains(teaPrefab))
            {
                Debug.Log(LogPrefix + "Tassen-Prefab steht schon in der Netzwerk-Prefab-Liste.");
            }
            else
            {
                networkPrefabs.Add(new NetworkPrefab { Prefab = teaPrefab });
                EditorUtility.SetDirty(networkPrefabs);
                AssetDatabase.SaveAssets();
                Debug.Log(LogPrefix + "Tassen-Prefab in die Netzwerk-Prefab-Liste eingetragen.");
            }

            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            CatalogReport report = CatalogValidator.Validate(catalog, networkPrefabs);
            Debug.Log(LogPrefix + report);
            return report.StartBlocked ? "Der Katalog hat Fehler:\n" + report : null;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
