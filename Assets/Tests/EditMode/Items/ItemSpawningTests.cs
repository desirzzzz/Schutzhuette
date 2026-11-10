using NUnit.Framework;
using Schutzhuette.Items;

namespace Schutzhuette.Tests
{
    /// <summary>
    /// Prüft die Regel, ob eine Art erscheinen darf (<see cref="ItemSpawning.CanSpawn"/>), ohne Szene
    /// und ohne Netz: GDD Gegenstandsarten, "Edge Cases" (ungültige Art erscheint nicht, ein Paket
    /// ohne feste Ware braucht eine Ware-Angabe, unbekannte Ware).
    /// </summary>
    public class ItemSpawningTests
    {
        private const string KindId = "testart";
        private const string PackId = "testpaket";
        private const string WareId = "ware_test";
        private const string OtherWareId = "ware_andere";

        // Grenzwerte: Genau diese Zahlen sind der Prüfpunkt.
        private const float ZeroMassKg = 0f;
        private const int ZeroPortions = 0;
        private const int SomePortions = 6;

        private CatalogTestFactory _factory;
        private WareKind _ware;

        [SetUp]
        public void SetUp()
        {
            _factory = new CatalogTestFactory();
            _ware = _factory.NewWare(WareId, "TestWare");
        }

        [TearDown]
        public void TearDown()
        {
            _factory.Dispose();
        }

        [Test]
        public void ValidKind_CanSpawnWithoutPackWare()
        {
            // Arrange
            ItemKind kind = _factory.NewKind(KindId, "TestArt");
            ItemCatalog catalog = _factory.NewCatalog(new[] { kind }, new[] { _ware });

            // Act
            bool canSpawn = ItemSpawning.CanSpawn(kind, null, catalog, out WareKind packWare, out string error);

            // Assert
            Assert.IsTrue(canSpawn, error);
            Assert.IsNull(packWare);
            Assert.IsNull(error);
        }

        [Test]
        public void NullKind_CannotSpawn()
        {
            ItemCatalog catalog = _factory.NewCatalog(wares: new[] { _ware });

            bool canSpawn = ItemSpawning.CanSpawn(null, null, catalog, out _, out string error);

            Assert.IsFalse(canSpawn);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void KindWithZeroMass_CannotSpawnAndErrorNamesId()
        {
            ItemKind kind = _factory.NewKind(KindId, "TestArt", massKg: ZeroMassKg);
            ItemCatalog catalog = _factory.NewCatalog(new[] { kind });

            bool canSpawn = ItemSpawning.CanSpawn(kind, null, catalog, out _, out string error);

            Assert.IsFalse(canSpawn);
            StringAssert.Contains(KindId, error);
        }

        [Test]
        public void KindWithoutPrefab_CannotSpawnAndErrorNamesId()
        {
            ItemKind kind = _factory.NewKind(KindId, "TestArt", withPrefab: false);
            ItemCatalog catalog = _factory.NewCatalog(new[] { kind });

            bool canSpawn = ItemSpawning.CanSpawn(kind, null, catalog, out _, out string error);

            Assert.IsFalse(canSpawn);
            StringAssert.Contains(KindId, error);
        }

        [Test]
        public void KindNotInCatalog_CannotSpawnAndErrorNamesId()
        {
            ItemKind kind = _factory.NewKind(KindId, "TestArt");
            ItemCatalog catalog = _factory.NewCatalog();

            bool canSpawn = ItemSpawning.CanSpawn(kind, null, catalog, out _, out string error);

            Assert.IsFalse(canSpawn);
            StringAssert.Contains(KindId, error);
        }

        [Test]
        public void NullCatalog_CannotSpawn()
        {
            ItemKind kind = _factory.NewKind(KindId, "TestArt");

            bool canSpawn = ItemSpawning.CanSpawn(kind, null, null, out _, out string error);

            Assert.IsFalse(canSpawn);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void PackWithoutFixedWareAndNoWareGiven_CannotSpawnAndErrorNamesId()
        {
            // Arrange: Wer ein Paket erzeugt, muss die Ware nennen.
            ItemKind pack = NewPack(null);
            ItemCatalog catalog = _factory.NewCatalog(new[] { pack }, new[] { _ware });

            // Act
            bool canSpawn = ItemSpawning.CanSpawn(pack, null, catalog, out WareKind packWare, out string error);

            // Assert
            Assert.IsFalse(canSpawn);
            Assert.IsNull(packWare);
            StringAssert.Contains(PackId, error);
        }

        [Test]
        public void PackWithoutFixedWareAndWareGiven_CanSpawnWithGivenWare()
        {
            ItemKind pack = NewPack(null);
            ItemCatalog catalog = _factory.NewCatalog(new[] { pack }, new[] { _ware });

            bool canSpawn = ItemSpawning.CanSpawn(pack, _ware, catalog, out WareKind packWare, out string error);

            Assert.IsTrue(canSpawn, error);
            Assert.AreSame(_ware, packWare);
        }

        [Test]
        public void PackWithFixedWare_CanSpawnWithFixedWareEvenIfAnotherIsGiven()
        {
            // Arrange: Die feste Ware der Art geht vor (der Kanister enthält immer Wasser).
            WareKind other = _factory.NewWare(OtherWareId, "TestWareAndere");
            ItemKind pack = NewPack(_ware);
            ItemCatalog catalog = _factory.NewCatalog(new[] { pack }, new[] { _ware, other });

            // Act
            bool canSpawn = ItemSpawning.CanSpawn(pack, other, catalog, out WareKind packWare, out string error);

            // Assert
            Assert.IsTrue(canSpawn, error);
            Assert.AreSame(_ware, packWare);
        }

        [Test]
        public void PackWithWareMissingFromCatalog_CannotSpawnAndErrorNamesWare()
        {
            // Arrange: Die Ware existiert, steht aber nicht im Katalog.
            WareKind unknown = _factory.NewWare(OtherWareId, "TestWareAndere");
            ItemKind pack = NewPack(null);
            ItemCatalog catalog = _factory.NewCatalog(new[] { pack }, new[] { _ware });

            // Act
            bool canSpawn = ItemSpawning.CanSpawn(pack, unknown, catalog, out WareKind packWare, out string error);

            // Assert
            Assert.IsFalse(canSpawn);
            Assert.IsNull(packWare);
            StringAssert.Contains(OtherWareId, error);
        }

        [Test]
        public void PackWithZeroPortions_CannotSpawnAndErrorNamesId()
        {
            ItemKind pack = _factory.NewKind(PackId, "TestPaket", category: ItemCategory.SupplyPack);
            pack.EditorSetSupplyPack(new SupplyPackBlock(true, ZeroPortions, _ware));
            ItemCatalog catalog = _factory.NewCatalog(new[] { pack }, new[] { _ware });

            bool canSpawn = ItemSpawning.CanSpawn(pack, null, catalog, out _, out string error);

            Assert.IsFalse(canSpawn);
            StringAssert.Contains(PackId, error);
        }

        private ItemKind NewPack(WareKind fixedWare)
        {
            ItemKind pack = _factory.NewKind(PackId, "TestPaket", category: ItemCategory.SupplyPack);
            pack.EditorSetSupplyPack(new SupplyPackBlock(true, SomePortions, fixedWare));
            return pack;
        }
    }
}
