using System.Text.RegularExpressions;
using NUnit.Framework;
using Schutzhuette.Items;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Schutzhuette.Tests
{
    /// <summary>
    /// Prüft das Nachschlagen im Laufzeit-Katalog (<see cref="CatalogLookup"/>) ohne Szene und ohne Netz:
    /// GDD Gegenstandsarten, Kriterium 27 (unbekannte Kennung: kein Treffer, genau ein Fehler,
    /// keine Exception).
    /// </summary>
    public class CatalogLookupTests
    {
        private const string KindId = "testart";
        private const string WareId = "ware_test";
        private const string NeedId = "beduerfnis_test";

        // Die Kennung aus Kriterium 27.
        private const string UnknownId = "gibt_es_nicht";

        private const int TooLongIdLength = 30;

        private CatalogTestFactory _factory;
        private ItemKind _kind;
        private WareKind _ware;
        private NeedKind _need;
        private CatalogLookup _lookup;

        [SetUp]
        public void SetUp()
        {
            _factory = new CatalogTestFactory();
            _kind = _factory.NewKind(KindId, "TestArt");
            _ware = _factory.NewWare(WareId, "TestWare");
            _need = _factory.NewNeed(NeedId, "TestBeduerfnis");
            ItemCatalog catalog = _factory.NewCatalog(new[] { _kind }, new[] { _ware }, new[] { _need });
            _lookup = new CatalogLookup(catalog);
        }

        [TearDown]
        public void TearDown()
        {
            _factory.Dispose();
        }

        [Test]
        public void KnownKindId_ReturnsTheKind()
        {
            bool found = _lookup.TryGetKind(new FixedString32Bytes(KindId), out ItemKind kind);

            Assert.IsTrue(found);
            Assert.AreSame(_kind, kind);
        }

        [Test]
        public void KnownWareId_ReturnsTheWare()
        {
            bool found = _lookup.TryGetWare(new FixedString32Bytes(WareId), out WareKind ware);

            Assert.IsTrue(found);
            Assert.AreSame(_ware, ware);
        }

        [Test]
        public void KnownNeedId_ReturnsTheNeed()
        {
            bool found = _lookup.TryGetNeed(new FixedString32Bytes(NeedId), out NeedKind need);

            Assert.IsTrue(found);
            Assert.AreSame(_need, need);
        }

        [Test]
        public void UnknownKindId_NoHitAndExactlyOneError()
        {
            // Arrange: genau ein Fehler mit der Kennung wird erwartet; ein zweiter ließe den Test scheitern.
            LogAssert.Expect(LogType.Error, new Regex(UnknownId));

            // Act
            bool found = _lookup.TryGetKind(new FixedString32Bytes(UnknownId), out ItemKind kind);

            // Assert
            Assert.IsFalse(found);
            Assert.IsNull(kind);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void UnknownWareId_NoHitAndExactlyOneError()
        {
            // Arrange: der Weg, über den ein Client die Ware eines Pakets nachschlägt.
            LogAssert.Expect(LogType.Error, new Regex(UnknownId));

            // Act
            bool found = _lookup.TryGetWare(new FixedString32Bytes(UnknownId), out WareKind ware);

            // Assert: nichts, was der Client anzeigen könnte.
            Assert.IsFalse(found);
            Assert.IsNull(ware);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void KindIdSearchedAsWare_NoHitAndExactlyOneError()
        {
            // Arrange: Arten und Waren sind getrennte Tabellen.
            LogAssert.Expect(LogType.Error, new Regex(KindId));

            // Act
            bool found = _lookup.TryGetWare(new FixedString32Bytes(KindId), out WareKind ware);

            // Assert
            Assert.IsFalse(found);
            Assert.IsNull(ware);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void NullCatalog_LookupMissesWithoutException()
        {
            // Arrange
            var emptyLookup = new CatalogLookup(null);
            LogAssert.Expect(LogType.Error, new Regex(KindId));

            // Act
            bool found = emptyLookup.TryGetKind(new FixedString32Bytes(KindId), out ItemKind kind);

            // Assert
            Assert.IsFalse(found);
            Assert.IsNull(kind);
        }

        [Test]
        public void EntryWithTooLongId_IsSkippedWithoutException()
        {
            // Arrange: 30 Zeichen passen nicht in den Netz-Datentyp; die Katalogprüfung meldet das.
            ItemKind broken = _factory.NewKind(new string('a', TooLongIdLength), "TestArtZuLang");
            ItemCatalog catalog = _factory.NewCatalog(new[] { _kind, broken });

            // Act
            var lookup = new CatalogLookup(catalog);
            bool found = lookup.TryGetKind(new FixedString32Bytes(KindId), out ItemKind kind);

            // Assert: Der gültige Eintrag bleibt auffindbar.
            Assert.IsTrue(found);
            Assert.AreSame(_kind, kind);
        }
    }
}
