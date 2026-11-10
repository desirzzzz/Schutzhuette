using System.Collections.Generic;
using NUnit.Framework;
using Schutzhuette.Items;
using UnityEngine;

namespace Schutzhuette.Tests
{
    /// <summary>
    /// Prüft die Entscheidung "einmal melden, einmal entfernen" (<see cref="ImpactJudge"/>) ohne Szene,
    /// ohne Physik und ohne Netz: GDD Gegenstandsarten, Kriterium 14. Melden und Entfernen sind hier
    /// Zähler statt Meldedienst und Ausgabestelle.
    /// </summary>
    public class ImpactJudgeTests
    {
        // Werte aus Kriterium 14: zwei Kontakte mit 8 und 9 m/s bei einer Bruchgeschwindigkeit von 7.
        private const float BreakSpeed = 7.0f;
        private const float FirstContactSpeed = 8f;
        private const float SecondContactSpeed = 9f;
        private const float GentleSpeed = 5.01f;
        private const float MassKg = 0.5f;
        private const float Tolerance = 0.0001f;

        private const string ReportStep = "melden";
        private const string RemoveStep = "entfernen";

        private readonly List<ImpactReport> _reports = new List<ImpactReport>();
        private readonly List<string> _steps = new List<string>();
        private int _removeCount;

        [SetUp]
        public void SetUp()
        {
            _reports.Clear();
            _steps.Clear();
            _removeCount = 0;
        }

        [Test]
        public void TwoContactsInSameStep_RemovesOnceAndReportsOneBreak()
        {
            // Arrange: eine fallende Tasse
            ImpactJudge judge = NewJudge(isFragile: true);

            // Act: zwei Kontakte im selben Physikschritt
            judge.HandleImpact(null, Vector3.zero, FirstContactSpeed, true, null);
            judge.HandleImpact(null, Vector3.zero, SecondContactSpeed, true, null);

            // Assert: genau ein Entfernen, genau eine Meldung, und die meldet den Bruch
            Assert.AreEqual(1, _removeCount);
            Assert.AreEqual(1, _reports.Count);
            Assert.IsTrue(_reports[0].Broke);
            Assert.IsTrue(judge.IsBroken);
        }

        [Test]
        public void BreakingImpact_ReportsBeforeRemoving()
        {
            ImpactJudge judge = NewJudge(isFragile: true);

            judge.HandleImpact(null, Vector3.zero, FirstContactSpeed, true, null);

            // Erst melden, dann entfernen: So kommt die Scherben-Meldung vor dem Entfernen bei den Clients an.
            CollectionAssert.AreEqual(new[] { ReportStep, RemoveStep }, _steps);
        }

        [Test]
        public void ImpactBelowBreakSpeed_ReportsWithoutBreakAndDoesNotRemove()
        {
            ImpactJudge judge = NewJudge(isFragile: true);

            judge.HandleImpact(null, Vector3.zero, GentleSpeed, true, null);

            Assert.AreEqual(0, _removeCount);
            Assert.AreEqual(1, _reports.Count);
            Assert.IsFalse(_reports[0].Broke);
            Assert.IsFalse(judge.IsBroken);
        }

        [Test]
        public void FastImpactWhileNotFalling_DoesNotBreak()
        {
            // Eine liegende oder getragene Tasse zerbricht nicht.
            ImpactJudge judge = NewJudge(isFragile: true);

            judge.HandleImpact(null, Vector3.zero, SecondContactSpeed, false, null);

            Assert.AreEqual(0, _removeCount);
            Assert.IsFalse(_reports[0].Broke);
        }

        [Test]
        public void FastImpactOfNonFragileItem_ReportsButDoesNotBreak()
        {
            // Eine Kiste meldet ihren Aufprall, zerbricht aber nie.
            ImpactJudge judge = NewJudge(isFragile: false);

            judge.HandleImpact(null, Vector3.zero, SecondContactSpeed, true, null);

            Assert.AreEqual(0, _removeCount);
            Assert.AreEqual(1, _reports.Count);
            Assert.IsFalse(_reports[0].Broke);
        }

        [Test]
        public void Report_CarriesTheMeasuredValues()
        {
            // Arrange
            var point = new Vector3(1f, 2f, 3f);
            ImpactJudge judge = NewJudge(isFragile: true);

            // Act
            judge.HandleImpact(null, point, GentleSpeed, true, null);

            // Assert
            ImpactReport report = _reports[0];
            Assert.AreEqual(point, report.Point);
            Assert.AreEqual(GentleSpeed, report.ImpactSpeed, Tolerance);
            Assert.AreEqual(MassKg, report.Mass, Tolerance);
            Assert.IsTrue(report.WasFalling);
        }

        [Test]
        public void SeveralGentleImpacts_EachOneIsReported()
        {
            // Ohne Bruch gibt es keine Sperre: Jeder Aufprall wird gemeldet.
            ImpactJudge judge = NewJudge(isFragile: true);

            judge.HandleImpact(null, Vector3.zero, GentleSpeed, true, null);
            judge.HandleImpact(null, Vector3.zero, GentleSpeed, true, null);

            Assert.AreEqual(2, _reports.Count);
            Assert.AreEqual(0, _removeCount);
        }

        private ImpactJudge NewJudge(bool isFragile)
        {
            return new ImpactJudge(isFragile, BreakSpeed, MassKg, RecordReport, RecordRemove);
        }

        private void RecordReport(ImpactReport report)
        {
            _reports.Add(report);
            _steps.Add(ReportStep);
        }

        private void RecordRemove()
        {
            _removeCount++;
            _steps.Add(RemoveStep);
        }
    }
}
