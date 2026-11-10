using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Schutzhuette.Items;
using UnityEngine;
using UnityEngine.TestTools;

namespace Schutzhuette.Tests
{
    /// <summary>
    /// Prüft den Meldedienst für Aufpralle (<see cref="ImpactEvents"/>): Jeder Zuhörer wird gerufen, und
    /// ein Fehler bei einem Zuhörer hält die anderen nicht auf (ADR-0001, Entscheidung 4).
    /// </summary>
    public class ImpactEventsTests
    {
        private const float ImpactSpeed = 8f;
        private const float MassKg = 0.5f;
        private const string ListenerError = "Testfehler im Zuhörer";

        private readonly List<Action<ImpactReport>> _subscribed = new List<Action<ImpactReport>>();

        [TearDown]
        public void TearDown()
        {
            // Jeder Test räumt seine Zuhörer wieder ab, damit kein Test den nächsten beeinflusst.
            foreach (Action<ImpactReport> listener in _subscribed)
            {
                ImpactEvents.Reported -= listener;
            }

            _subscribed.Clear();
        }

        [Test]
        public void Raise_CallsEveryListenerWithTheReport()
        {
            // Arrange
            var received = new List<float>();
            Subscribe(report => received.Add(report.ImpactSpeed));
            Subscribe(report => received.Add(report.Mass));

            // Act
            ImpactEvents.Raise(NewReport());

            // Assert
            CollectionAssert.AreEqual(new[] { ImpactSpeed, MassKg }, received);
        }

        [Test]
        public void ListenerThrows_OtherListenersAreStillCalledAndTheErrorIsLogged()
        {
            // Arrange: Der erste Zuhörer wirft, der zweite muss trotzdem drankommen.
            bool secondCalled = false;
            Subscribe(report => throw new InvalidOperationException(ListenerError));
            Subscribe(report => secondCalled = true);
            LogAssert.Expect(LogType.Exception, new Regex(ListenerError));

            // Act
            Assert.DoesNotThrow(() => ImpactEvents.Raise(NewReport()));

            // Assert
            Assert.IsTrue(secondCalled);
        }

        [Test]
        public void RaiseWithoutListeners_DoesNothing()
        {
            Assert.DoesNotThrow(() => ImpactEvents.Raise(NewReport()));
        }

        private void Subscribe(Action<ImpactReport> listener)
        {
            ImpactEvents.Reported += listener;
            _subscribed.Add(listener);
        }

        private static ImpactReport NewReport()
        {
            return new ImpactReport(null, Vector3.zero, ImpactSpeed, MassKg, true, false, null);
        }
    }
}
