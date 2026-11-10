using System;
using System.Collections.Generic;
using Schutzhuette.Items;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Schutzhuette.Tests
{
    /// <summary>
    /// Baut Test-Kataloge für die Katalog-Tests: Arten, Waren, Bedürfnisse, Prefabs und eine
    /// Netzwerk-Prefab-Liste. Nichts davon liegt als Datei im Projekt.
    /// <see cref="Dispose"/> räumt alles wieder ab, damit kein Test den nächsten beeinflusst.
    /// </summary>
    internal sealed class CatalogTestFactory : IDisposable
    {
        /// <summary>Gültiger Anzeigename, wenn ein Test keinen eigenen braucht.</summary>
        public const string DefaultDisplayName = "Testeintrag";

        /// <summary>Gültige Masse, wenn ein Test keine eigene braucht.</summary>
        public const float DefaultMassKg = 1f;

        private readonly List<Object> _created = new List<Object>();
        private int _prefabCount;

        public CatalogTestFactory()
        {
            PrefabList = Track(ScriptableObject.CreateInstance<NetworkPrefabsList>());
        }

        /// <summary>Die Netzwerk-Prefab-Liste, in die <see cref="NewPrefab"/> einträgt.</summary>
        public NetworkPrefabsList PrefabList { get; }

        /// <summary>Ein Test-Prefab mit NetworkObject. <paramref name="listed"/> trägt es in die Liste ein.</summary>
        public NetworkObject NewPrefab(bool listed = true)
        {
            _prefabCount++;

            // HideAndDontSave: Das Objekt gehört zu keiner Szene und wird nie gespeichert.
            var gameObject = new GameObject("TestPrefab" + _prefabCount)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _created.Add(gameObject);

            NetworkObject networkObject = gameObject.AddComponent<NetworkObject>();

            // Das Etikett; welche Art darauf steht, trägt NewKind ein.
            gameObject.AddComponent<Item>();
            if (listed)
            {
                PrefabList.Add(new NetworkPrefab { Prefab = gameObject });
            }

            return networkObject;
        }

        /// <summary>
        /// Eine Test-Art. Ohne Angabe bekommt sie ein eigenes, eingetragenes Prefab;
        /// <paramref name="withPrefab"/> = false lässt das Prefab weg.
        /// </summary>
        public ItemKind NewKind(string id, string assetName, string displayName = DefaultDisplayName,
            float massKg = DefaultMassKg, CarryMode carryMode = CarryMode.Solo,
            ItemCategory category = ItemCategory.RawMaterial, NetworkObject prefab = null, bool withPrefab = true)
        {
            ItemKind kind = Track(ScriptableObject.CreateInstance<ItemKind>());
            kind.name = assetName;

            NetworkObject usedPrefab = prefab;
            if (usedPrefab == null && withPrefab)
            {
                usedPrefab = NewPrefab();
            }

            kind.EditorSetBasics(id, displayName, massKg, carryMode, category, ImpactSoundGroup.Wood, usedPrefab);

            // Das Etikett des Prefabs zeigt auf die erste Art, die es benutzt. Teilen sich zwei Arten
            // ein Prefab, zeigt es für die zweite auf die falsche Art, wie im echten Projekt.
            if (usedPrefab != null && usedPrefab.TryGetComponent(out Item item) && item.Kind == null)
            {
                item.EditorSetKind(kind);
            }

            return kind;
        }

        /// <summary>Eine Test-Ware.</summary>
        public WareKind NewWare(string id, string assetName, string displayName = DefaultDisplayName)
        {
            WareKind ware = Track(ScriptableObject.CreateInstance<WareKind>());
            ware.name = assetName;
            ware.EditorSet(id, displayName);
            return ware;
        }

        /// <summary>Ein Test-Bedürfnis.</summary>
        public NeedKind NewNeed(string id, string assetName, string displayName = DefaultDisplayName)
        {
            NeedKind need = Track(ScriptableObject.CreateInstance<NeedKind>());
            need.name = assetName;
            need.EditorSet(id, displayName);
            return need;
        }

        /// <summary>Ein Test-Katalog mit den übergebenen Einträgen und den Standard-Einstellungen.</summary>
        public ItemCatalog NewCatalog(IEnumerable<ItemKind> kinds = null, IEnumerable<WareKind> wares = null,
            IEnumerable<NeedKind> needs = null)
        {
            ItemCatalog catalog = Track(ScriptableObject.CreateInstance<ItemCatalog>());
            catalog.name = "TestKatalog";
            catalog.EditorSetContents(kinds, wares, needs);
            return catalog;
        }

        /// <summary>Zerstört alles, was diese Fabrik erzeugt hat.</summary>
        public void Dispose()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }

            _created.Clear();
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
