using System.Collections.Generic;
using NUnit.Framework;
using Schutzhuette.Items;
using Unity.Netcode;
using UnityEditor;

namespace Schutzhuette.Tests
{
    /// <summary>
    /// Schnappschuss des ausgelieferten Katalogs (GDD Gegenstandsarten, Kriterium 1): keine Fehler,
    /// die Arten haben genau die Werte aus der Tabelle "Startkatalog", und es gibt genau die sieben Waren.
    /// Ändert ein Dokument einen Wert, werden Tabelle und Test zusammen geändert.
    /// </summary>
    /// <remarks>
    /// Stand Teil 1 der Story 005: vier Arten. Mit Teil 2 wächst <see cref="ExpectedKinds"/> auf 13.
    /// Der Test liest die echten Asset-Dateien; das ist bei einem Schnappschuss der Zweck.
    /// </remarks>
    public class StartCatalogTests
    {
        private const string CatalogPath = "Assets/_Project/Data/Items/ItemCatalog.asset";
        private const string NetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";
        private const float Tolerance = 0.0001f;

        private static readonly string[] ExpectedWareIds =
        {
            "ware_speck", "ware_brot", "ware_mehl", "ware_eier", "ware_milch", "ware_tee", "ware_wasser",
        };

        // Zeilen der Tabelle "Startkatalog für den Vertical Slice" im GDD.
        private static readonly ExpectedKind[] ExpectedKinds =
        {
            new ExpectedKind("vorratskiste", "Vorratskiste", 10f, CarryMode.Solo, ItemCategory.SupplyPack,
                ImpactSoundGroup.Wood) { Portions = 6 },
            new ExpectedKind("holzscheit", "Holzscheit", 3f, CarryMode.Solo, ItemCategory.Fuel,
                ImpactSoundGroup.Wood) { BurnSeconds = 45f },
            new ExpectedKind("truhe", "Vorratstruhe", 80f, CarryMode.Pair, ItemCategory.SupplyPack,
                ImpactSoundGroup.Wood) { Portions = 20 },
            new ExpectedKind("tee", "Tasse Tee", 0.5f, CarryMode.Solo, ItemCategory.Dish,
                ImpactSoundGroup.Soft) { IsDish = true, CoolTimeSeconds = 90f, BreakSpeed = 7.0f },
        };

        private ItemCatalog _catalog;
        private NetworkPrefabsList _networkPrefabs;

        [OneTimeSetUp]
        public void LoadShippedCatalog()
        {
            _catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            _networkPrefabs = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NetworkPrefabsPath);
            Assert.IsNotNull(_catalog, $"Katalog fehlt: {CatalogPath}");
            Assert.IsNotNull(_networkPrefabs, $"Netzwerk-Prefab-Liste fehlt: {NetworkPrefabsPath}");
        }

        [Test]
        public void ShippedCatalog_ValidationFindsNoErrors()
        {
            CatalogReport report = CatalogValidator.Validate(_catalog, _networkPrefabs);

            Assert.IsEmpty(report.Errors, report.ToString());
            Assert.IsFalse(report.StartBlocked);
        }

        [Test]
        public void ShippedCatalog_HasExactlyTheSevenWares()
        {
            var actualIds = new List<string>();
            foreach (WareKind ware in _catalog.Wares)
            {
                actualIds.Add(ware.Id);
            }

            CollectionAssert.AreEquivalent(ExpectedWareIds, actualIds);
        }

        [Test]
        public void ShippedCatalog_HasExactlyTheKindsOfTheStartCatalogTable()
        {
            var expectedIds = new List<string>();
            foreach (ExpectedKind expected in ExpectedKinds)
            {
                expectedIds.Add(expected.Id);
            }

            var actualIds = new List<string>();
            foreach (ItemKind kind in _catalog.Kinds)
            {
                actualIds.Add(kind.Id);
            }

            CollectionAssert.AreEquivalent(expectedIds, actualIds);
        }

        [TestCaseSource(nameof(ExpectedKindCases))]
        public void ShippedKind_MatchesStartCatalogRow(ExpectedKind expected)
        {
            // Arrange
            ItemKind kind = FindKind(expected.Id);
            Assert.IsNotNull(kind, $"Art „{expected.Id}“ fehlt im Katalog.");

            // Assert: Pflichtfelder
            Assert.AreEqual(expected.DisplayName, kind.DisplayName);
            Assert.AreEqual(expected.MassKg, kind.MassKg, Tolerance);
            Assert.AreEqual(expected.CarryMode, kind.CarryMode);
            Assert.AreEqual(expected.Category, kind.Category);
            Assert.AreEqual(expected.ImpactSound, kind.ImpactSound);
            Assert.IsNotNull(kind.Prefab, "Prefab fehlt.");

            // Assert: Eigenschaften, an oder aus genau wie in der Tabelle
            Assert.AreEqual(expected.BurnSeconds > 0f, kind.Fuel.Enabled, "brennbar");
            Assert.AreEqual(expected.BurnSeconds, kind.Fuel.BurnSeconds, Tolerance);

            Assert.AreEqual(expected.Portions > 0, kind.SupplyPack.Enabled, "Vorratspaket");
            Assert.AreEqual(expected.Portions, kind.SupplyPack.Portions);
            Assert.AreEqual(expected.FixedWareId, kind.SupplyPack.FixedWare != null ? kind.SupplyPack.FixedWare.Id : null);

            Assert.AreEqual(expected.IsDish, kind.Dish.Enabled, "Gericht");
            Assert.AreEqual(expected.CoolTimeSeconds, kind.Dish.CoolTimeSeconds, Tolerance);

            Assert.AreEqual(expected.BreakSpeed > 0f, kind.Fragile.Enabled, "zerbrechlich");
            Assert.AreEqual(expected.BreakSpeed, kind.Fragile.BreakSpeed, Tolerance);

            Assert.AreEqual(expected.ChopCount > 0, kind.Chop.Enabled, "hackbar");
            Assert.AreEqual(expected.ChopCount, kind.Chop.Count);
            Assert.AreEqual(expected.ChopResultId, kind.Chop.Result != null ? kind.Chop.Result.Id : null);

            Assert.AreEqual(expected.ToolAction.HasValue, kind.Tool.Enabled, "Werkzeug");
            if (expected.ToolAction.HasValue)
            {
                Assert.AreEqual(expected.ToolAction.Value, kind.Tool.Action);
            }

            Assert.AreEqual(expected.EquipmentSlot.HasValue, kind.Equipment.Enabled, "Ausrüstung");
            if (expected.EquipmentSlot.HasValue)
            {
                Assert.AreEqual(expected.EquipmentSlot.Value, kind.Equipment.Slot);
            }
        }

        private static IEnumerable<TestCaseData> ExpectedKindCases()
        {
            foreach (ExpectedKind expected in ExpectedKinds)
            {
                yield return new TestCaseData(expected).SetName($"ShippedKind_MatchesStartCatalogRow({expected.Id})");
            }
        }

        private ItemKind FindKind(string id)
        {
            foreach (ItemKind kind in _catalog.Kinds)
            {
                if (kind != null && kind.Id == id)
                {
                    return kind;
                }
            }

            return null;
        }

        /// <summary>Eine Zeile der Tabelle "Startkatalog". Was nicht gesetzt ist, hat die Art nicht.</summary>
        public sealed class ExpectedKind
        {
            public ExpectedKind(string id, string displayName, float massKg, CarryMode carryMode,
                ItemCategory category, ImpactSoundGroup impactSound)
            {
                Id = id;
                DisplayName = displayName;
                MassKg = massKg;
                CarryMode = carryMode;
                Category = category;
                ImpactSound = impactSound;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public float MassKg { get; }
            public CarryMode CarryMode { get; }
            public ItemCategory Category { get; }
            public ImpactSoundGroup ImpactSound { get; }

            public float BurnSeconds { get; set; }
            public int Portions { get; set; }
            public string FixedWareId { get; set; }
            public bool IsDish { get; set; }
            public float CoolTimeSeconds { get; set; }
            public float BreakSpeed { get; set; }
            public int ChopCount { get; set; }
            public string ChopResultId { get; set; }
            public ToolAction? ToolAction { get; set; }
            public EquipmentSlot? EquipmentSlot { get; set; }

            public override string ToString() => Id;
        }
    }
}
