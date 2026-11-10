using NUnit.Framework;
using Schutzhuette.Items;
using UnityEngine;

namespace Schutzhuette.Tests
{
    /// <summary>
    /// Prüft die Rechenregeln für das Zerbrechen (<see cref="BreakRule"/>) und den Hinweis der
    /// Katalogprüfung zur Bruchgeschwindigkeit: GDD Gegenstandsarten, Kriterien 10 bis 13.
    /// Ohne Szene, ohne Physik, ohne Netz.
    /// </summary>
    public class BreakRuleTests
    {
        // Werte aus den Abnahmekriterien: Genau diese Zahlen sind der Prüfpunkt.
        private const float BreakSpeed = 7.0f;
        private const float HoldHeight = 1.28f;
        private const float ThrowSpeed = 8.0f;
        private const float Gravity = 9.81f;
        private const float FastImpact = 12.5f;

        // Toleranz aus dem GDD: ±0,01 bei m/s.
        private const float SpeedTolerance = 0.01f;

        // Wortlaut der Hinweise aus Kriterium 13.
        private const string PutDownHint = "Ablegen zerbricht";
        private const string StraightThrowHint = "gerade Würfe zerbrechen nicht";

        // Übrige Katalog-Einstellungen, für diese Tests ohne Bedeutung.
        private const float MaxSoloMassKg = 25f;
        private const float FloorFactor = 6f;
        private const float MaxPathMeters = 15f;
        private const float WalkSpeedMin = 4f;
        private const float ChopTolerance = 0.1f;

        private const string KindId = "testtasse";
        private const string KindAsset = "TestTasse";

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

        // ---------- Kriterium 10: nur der senkrechte Anteil zählt ----------

        [Test]
        public void FlatThrowOntoFloor_ImpactSpeedIsNormalPartAndCupStaysWhole()
        {
            // Arrange: flacher Wurf, Gesamtbetrag 7,82 m/s, senkrecht nur 5,01 m/s.
            var relativeVelocity = new Vector3(6.0f, -5.01f, 0f);
            Vector3 floorNormal = Vector3.up;

            // Act
            float impactSpeed = BreakRule.ImpactSpeed(relativeVelocity, floorNormal);
            bool breaks = BreakRule.BreaksOnImpact(true, true, impactSpeed, BreakSpeed);

            // Assert
            Assert.AreEqual(5.01f, impactSpeed, SpeedTolerance);
            Assert.IsFalse(breaks);
        }

        // ---------- Kriterium 11: Grenze, schräger Wurf, nicht zerbrechlich, getragen ----------

        [TestCase(4.2f, false)]
        [TestCase(6.1f, false)]
        [TestCase(7.0f, false)]
        [TestCase(7.01f, true)]
        [TestCase(8.0f, true)]
        [TestCase(12.5f, true)]
        public void FragileAndFalling_BreaksOnlyAboveBreakSpeed(float impactSpeed, bool expectedBreaks)
        {
            bool breaks = BreakRule.BreaksOnImpact(true, true, impactSpeed, BreakSpeed);

            Assert.AreEqual(expectedBreaks, breaks);
        }

        [Test]
        public void ObliqueThrowAgainstWall_ImpactSpeedIsNormalPartAndCupStaysWhole()
        {
            // Arrange: 8 m/s, 40° schräg zur Wand.
            var relativeVelocity = new Vector3(5.14f, 0f, 6.13f);
            Vector3 wallNormal = Vector3.forward;

            // Act
            float impactSpeed = BreakRule.ImpactSpeed(relativeVelocity, wallNormal);
            bool breaks = BreakRule.BreaksOnImpact(true, true, impactSpeed, BreakSpeed);

            // Assert
            Assert.AreEqual(6.13f, impactSpeed, SpeedTolerance);
            Assert.IsFalse(breaks);
        }

        [Test]
        public void NotFragile_NeverBreaks()
        {
            // Speckbrot: nicht zerbrechlich, auch bei hartem Aufprall heil.
            bool breaks = BreakRule.BreaksOnImpact(false, true, FastImpact, BreakSpeed);

            Assert.IsFalse(breaks);
        }

        [Test]
        public void CarriedNotFalling_NeverBreaks()
        {
            // Eine getragene Tasse zerbricht nie, egal wie schnell der Kontakt ist.
            bool breaks = BreakRule.BreaksOnImpact(true, false, FastImpact, BreakSpeed);

            Assert.IsFalse(breaks);
        }

        // ---------- Kriterium 12: Fallgeschwindigkeit beim Loslassen ----------

        [TestCase(1.28f, 0f, 5.01f)]
        [TestCase(1.28f, 0.9f, 2.73f)]
        [TestCase(1.8f, 0f, 5.94f)]
        [TestCase(1.28f, 2.0f, 0f)]
        public void ReleaseFallSpeed_MatchesFallFromHoldHeight(float holdHeight, float surfaceHeight,
            float expectedSpeed)
        {
            float speed = BreakRule.ReleaseFallSpeed(holdHeight, surfaceHeight, Gravity);

            Assert.IsFalse(float.IsNaN(speed));
            Assert.AreEqual(expectedSpeed, speed, SpeedTolerance);
        }

        // ---------- Kriterium 13: Hinweise der Katalogprüfung, nie ein Fehler ----------

        [TestCase(7.0f)]
        [TestCase(5.52f)]
        [TestCase(7.20f)]
        public void BreakSpeedInsideWindow_NoHintAndNoError(float breakSpeed)
        {
            CatalogReport report = ValidateFragileKind(breakSpeed);

            Assert.IsEmpty(report.Hints, report.ToString());
            Assert.IsEmpty(report.Errors, report.ToString());
        }

        [Test]
        public void BreakSpeedBelowWindow_OneHintPutDownBreaksAndNoError()
        {
            const float tooLow = 5.45f;

            CatalogReport report = ValidateFragileKind(tooLow);

            Assert.AreEqual(1, report.Hints.Count, report.ToString());
            StringAssert.Contains(PutDownHint, report.Hints[0]);
            StringAssert.Contains(KindId, report.Hints[0]);
            Assert.IsEmpty(report.Errors, report.ToString());
            Assert.IsFalse(report.StartBlocked);
        }

        [Test]
        public void BreakSpeedAboveWindow_OneHintStraightThrowsDoNotBreakAndNoError()
        {
            const float tooHigh = 7.25f;

            CatalogReport report = ValidateFragileKind(tooHigh);

            Assert.AreEqual(1, report.Hints.Count, report.ToString());
            StringAssert.Contains(StraightThrowHint, report.Hints[0]);
            StringAssert.Contains(KindId, report.Hints[0]);
            Assert.IsEmpty(report.Errors, report.ToString());
            Assert.IsFalse(report.StartBlocked);
        }

        [Test]
        public void FragileSwitchedOff_NoHintEvenWithBreakSpeedOutsideWindow()
        {
            // Arrange: Der Wert liegt außerhalb des Fensters, aber die Eigenschaft ist aus.
            const float tooLow = 1f;
            ItemKind kind = _factory.NewKind(KindId, KindAsset, category: ItemCategory.Dish);
            kind.EditorSetFragile(new FragileBlock(false, tooLow));

            // Act
            CatalogReport report = Validate(kind);

            // Assert
            Assert.IsEmpty(report.Hints, report.ToString());
        }

        // ---------- Helfer ----------

        private CatalogReport ValidateFragileKind(float breakSpeed)
        {
            ItemKind kind = _factory.NewKind(KindId, KindAsset, category: ItemCategory.Dish);
            kind.EditorSetFragile(new FragileBlock(true, breakSpeed));
            return Validate(kind);
        }

        private CatalogReport Validate(ItemKind kind)
        {
            ItemCatalog catalog = _factory.NewCatalog(new[] { kind });

            // Prüfwerte ausdrücklich setzen, damit ein geänderter Standardwert keinen Test bricht.
            catalog.Settings.EditorSet(MaxSoloMassKg, HoldHeight, ThrowSpeed, FloorFactor, MaxPathMeters,
                WalkSpeedMin, ChopTolerance);

            return CatalogValidator.Validate(catalog, _factory.PrefabList);
        }
    }
}
