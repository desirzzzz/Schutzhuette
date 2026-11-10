using NUnit.Framework;
using Schutzhuette.Items;
using Unity.Netcode;

namespace Schutzhuette.Tests
{
    /// <summary>
    /// Prüft die Grundregeln der Katalogprüfung (<see cref="CatalogValidator.Validate"/>) ohne Szene
    /// und ohne Netz: GDD Gegenstandsarten, Kriterien 2 und 5, Edge Cases für Waren, dazu das
    /// Kennungsmuster und die Prefab-Regeln aus ADR-0002.
    /// </summary>
    public class CatalogValidatorTests
    {
        private const string KindId = "testart";
        private const string KindAsset = "TestArt";
        private const string SecondKindAsset = "TestArtZwei";
        private const string WareId = "ware_test";
        private const string WareAsset = "TestWare";
        private const string SecondWareAsset = "TestWareZwei";
        private const string NeedId = "beduerfnis_test";
        private const string NeedAsset = "TestBeduerfnis";

        // Grenzwerte: Genau diese Zahlen sind der Prüfpunkt.
        private const float ZeroMassKg = 0f;
        private const float NegativeMassKg = -1f;
        private const int ZeroPortions = 0;
        private const int SomePortions = 6;
        private const int LongestValidIdLength = 29;
        private const int TooLongIdLength = 30;

        private CatalogTestFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new CatalogTestFactory();
        }

        [TearDown]
        public void TearDown()
        {
            _factory.Dispose();
        }

        // ---------- Gültiger Katalog ----------

        [Test]
        public void ValidCatalog_NoErrorsAndStartNotBlocked()
        {
            // Arrange
            WareKind ware = _factory.NewWare(WareId, WareAsset);
            NeedKind need = _factory.NewNeed(NeedId, NeedAsset);
            ItemKind plain = _factory.NewKind(KindId, KindAsset);
            ItemKind pack = _factory.NewKind("testpaket", "TestPaket", category: ItemCategory.SupplyPack);
            pack.EditorSetSupplyPack(new SupplyPackBlock(true, SomePortions, ware));
            ItemCatalog catalog = _factory.NewCatalog(new[] { plain, pack }, new[] { ware }, new[] { need });

            // Act
            CatalogReport report = CatalogValidator.Validate(catalog, _factory.PrefabList);

            // Assert
            Assert.IsEmpty(report.Errors, report.ToString());
            Assert.IsEmpty(report.Hints, report.ToString());
            Assert.IsFalse(report.StartBlocked);
        }

        // ---------- Kriterium 2: je ein Fehler, der Kennung oder Asset-Namen nennt ----------

        [Test]
        public void KindWithZeroMass_OneErrorNamingId()
        {
            ItemKind kind = _factory.NewKind(KindId, KindAsset, massKg: ZeroMassKg);

            CatalogReport report = ValidateKinds(kind);

            AssertSingleErrorNaming(report, KindId);
        }

        [Test]
        public void KindWithNegativeMass_OneErrorNamingId()
        {
            ItemKind kind = _factory.NewKind(KindId, KindAsset, massKg: NegativeMassKg);

            CatalogReport report = ValidateKinds(kind);

            AssertSingleErrorNaming(report, KindId);
        }

        [Test]
        public void KindWithEmptyId_OneErrorNamingAsset()
        {
            ItemKind kind = _factory.NewKind(string.Empty, KindAsset);

            CatalogReport report = ValidateKinds(kind);

            AssertSingleErrorNaming(report, KindAsset);
        }

        [Test]
        public void KindWithEmptyDisplayName_OneErrorNamingId()
        {
            ItemKind kind = _factory.NewKind(KindId, KindAsset, displayName: string.Empty);

            CatalogReport report = ValidateKinds(kind);

            AssertSingleErrorNaming(report, KindId);
        }

        [Test]
        public void KindWithoutPrefab_OneErrorNamingId()
        {
            ItemKind kind = _factory.NewKind(KindId, KindAsset, withPrefab: false);

            CatalogReport report = ValidateKinds(kind);

            AssertSingleErrorNaming(report, KindId);
        }

        [Test]
        public void SupplyPackWithZeroPortions_OneErrorNamingId()
        {
            // Arrange
            WareKind ware = _factory.NewWare(WareId, WareAsset);
            ItemKind pack = _factory.NewKind(KindId, KindAsset, category: ItemCategory.SupplyPack);
            pack.EditorSetSupplyPack(new SupplyPackBlock(true, ZeroPortions, ware));
            ItemCatalog catalog = _factory.NewCatalog(new[] { pack }, new[] { ware });

            // Act
            CatalogReport report = CatalogValidator.Validate(catalog, _factory.PrefabList);

            // Assert
            AssertSingleErrorNaming(report, KindId);
        }

        [Test]
        public void SupplyPackWithUnknownFixedWare_OneErrorNamingId()
        {
            // Arrange: Die Ware existiert, steht aber nicht im Katalog.
            WareKind unknownWare = _factory.NewWare(WareId, WareAsset);
            ItemKind pack = _factory.NewKind(KindId, KindAsset, category: ItemCategory.SupplyPack);
            pack.EditorSetSupplyPack(new SupplyPackBlock(true, SomePortions, unknownWare));
            ItemCatalog catalog = _factory.NewCatalog(new[] { pack });

            // Act
            CatalogReport report = CatalogValidator.Validate(catalog, _factory.PrefabList);

            // Assert
            AssertSingleErrorNaming(report, KindId);
        }

        [Test]
        public void SupplyPackWithoutFixedWare_NoError()
        {
            // Arrange: Ohne feste Ware legt fest, wer das Paket erzeugt, was drin ist.
            ItemKind pack = _factory.NewKind(KindId, KindAsset, category: ItemCategory.SupplyPack);
            pack.EditorSetSupplyPack(new SupplyPackBlock(true, SomePortions, null));

            // Act
            CatalogReport report = ValidateKinds(pack);

            // Assert
            Assert.IsEmpty(report.Errors, report.ToString());
        }

        // ---------- Kriterium 5: doppelte Kennung ----------

        [Test]
        public void TwoKindsWithSameId_OneErrorNamingBothAssetsAndStartBlocked()
        {
            // Arrange
            ItemKind first = _factory.NewKind(KindId, KindAsset);
            ItemKind second = _factory.NewKind(KindId, SecondKindAsset);

            // Act
            CatalogReport report = ValidateKinds(first, second);

            // Assert
            Assert.AreEqual(1, report.Errors.Count, report.ToString());
            StringAssert.Contains(KindAsset, report.Errors[0]);
            StringAssert.Contains(SecondKindAsset, report.Errors[0]);
            Assert.IsTrue(report.StartBlocked);
        }

        [Test]
        public void TwoWaresWithSameId_OneErrorNamingBothAssetsAndStartBlocked()
        {
            // Arrange
            WareKind first = _factory.NewWare(WareId, WareAsset);
            WareKind second = _factory.NewWare(WareId, SecondWareAsset);
            ItemCatalog catalog = _factory.NewCatalog(wares: new[] { first, second });

            // Act
            CatalogReport report = CatalogValidator.Validate(catalog, _factory.PrefabList);

            // Assert
            Assert.AreEqual(1, report.Errors.Count, report.ToString());
            StringAssert.Contains(WareAsset, report.Errors[0]);
            StringAssert.Contains(SecondWareAsset, report.Errors[0]);
            Assert.IsTrue(report.StartBlocked);
        }

        // ---------- Edge Case: Waren ----------

        [Test]
        public void WareWithEmptyId_OneErrorNamingAsset()
        {
            WareKind ware = _factory.NewWare(string.Empty, WareAsset);
            ItemCatalog catalog = _factory.NewCatalog(wares: new[] { ware });

            CatalogReport report = CatalogValidator.Validate(catalog, _factory.PrefabList);

            AssertSingleErrorNaming(report, WareAsset);
        }

        [Test]
        public void WareWithEmptyDisplayName_OneErrorNamingId()
        {
            WareKind ware = _factory.NewWare(WareId, WareAsset, displayName: string.Empty);
            ItemCatalog catalog = _factory.NewCatalog(wares: new[] { ware });

            CatalogReport report = CatalogValidator.Validate(catalog, _factory.PrefabList);

            AssertSingleErrorNaming(report, WareId);
        }

        // ---------- Kennungsmuster (ADR-0002, Verification 4) ----------

        [Test]
        public void IdWith29Characters_NoError()
        {
            string id = new string('a', LongestValidIdLength);
            ItemKind kind = _factory.NewKind(id, KindAsset);

            CatalogReport report = ValidateKinds(kind);

            Assert.IsEmpty(report.Errors, report.ToString());
        }

        [Test]
        public void IdWith30Characters_OneErrorNamingAsset()
        {
            string id = new string('a', TooLongIdLength);
            ItemKind kind = _factory.NewKind(id, KindAsset);

            CatalogReport report = ValidateKinds(kind);

            AssertSingleErrorNaming(report, KindAsset);
        }

        [Test]
        public void IdWithUppercaseLetter_OneErrorNamingAsset()
        {
            ItemKind kind = _factory.NewKind("Testart", KindAsset);

            CatalogReport report = ValidateKinds(kind);

            AssertSingleErrorNaming(report, KindAsset);
        }

        [Test]
        public void KindIdWithWarePrefix_OneErrorNamingId()
        {
            ItemKind kind = _factory.NewKind(WareId, KindAsset);

            CatalogReport report = ValidateKinds(kind);

            AssertSingleErrorNaming(report, WareId);
        }

        [Test]
        public void WareIdWithoutWarePrefix_OneErrorNamingId()
        {
            WareKind ware = _factory.NewWare(KindId, WareAsset);
            ItemCatalog catalog = _factory.NewCatalog(wares: new[] { ware });

            CatalogReport report = CatalogValidator.Validate(catalog, _factory.PrefabList);

            AssertSingleErrorNaming(report, KindId);
        }

        [Test]
        public void NeedIdWithoutNeedPrefix_OneErrorNamingId()
        {
            NeedKind need = _factory.NewNeed(KindId, NeedAsset);
            ItemCatalog catalog = _factory.NewCatalog(needs: new[] { need });

            CatalogReport report = CatalogValidator.Validate(catalog, _factory.PrefabList);

            AssertSingleErrorNaming(report, KindId);
        }

        // ---------- Prefab-Regeln (ADR-0002, Entscheidung 3) ----------

        [Test]
        public void PrefabMissingFromNetworkPrefabList_OneErrorNamingId()
        {
            NetworkObject unlisted = _factory.NewPrefab(listed: false);
            ItemKind kind = _factory.NewKind(KindId, KindAsset, prefab: unlisted);

            CatalogReport report = ValidateKinds(kind);

            AssertSingleErrorNaming(report, KindId);
        }

        [Test]
        public void TwoKindsSharingOnePrefab_OneErrorNamingBothIds()
        {
            // Arrange
            const string secondId = "testart_zwei";
            NetworkObject shared = _factory.NewPrefab();
            ItemKind first = _factory.NewKind(KindId, KindAsset, prefab: shared);
            ItemKind second = _factory.NewKind(secondId, SecondKindAsset, prefab: shared);

            // Act
            CatalogReport report = ValidateKinds(first, second);

            // Assert
            Assert.AreEqual(1, report.Errors.Count, report.ToString());
            StringAssert.Contains(KindId, report.Errors[0]);
            StringAssert.Contains(secondId, report.Errors[0]);
        }

        // ---------- Fehlende Eingaben ----------

        [Test]
        public void NullCatalog_OneErrorAndStartBlocked()
        {
            CatalogReport report = CatalogValidator.Validate(null, _factory.PrefabList);

            Assert.AreEqual(1, report.Errors.Count, report.ToString());
            Assert.IsTrue(report.StartBlocked);
        }

        [Test]
        public void NullNetworkPrefabList_OneErrorAndStartBlocked()
        {
            ItemKind kind = _factory.NewKind(KindId, KindAsset);
            ItemCatalog catalog = _factory.NewCatalog(new[] { kind });

            CatalogReport report = CatalogValidator.Validate(catalog, null);

            Assert.AreEqual(1, report.Errors.Count, report.ToString());
            Assert.IsTrue(report.StartBlocked);
        }

        // ---------- Helfer ----------

        private CatalogReport ValidateKinds(params ItemKind[] kinds)
        {
            ItemCatalog catalog = _factory.NewCatalog(kinds);
            return CatalogValidator.Validate(catalog, _factory.PrefabList);
        }

        private static void AssertSingleErrorNaming(CatalogReport report, string expectedName)
        {
            Assert.AreEqual(1, report.Errors.Count, report.ToString());
            StringAssert.Contains(expectedName, report.Errors[0]);
            Assert.IsTrue(report.StartBlocked);
        }
    }
}
