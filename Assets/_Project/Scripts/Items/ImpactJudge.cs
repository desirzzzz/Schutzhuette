using System;
using Unity.Netcode;
using UnityEngine;

namespace Schutzhuette.Items
{
    /// <summary>
    /// Entscheidet für einen einzelnen Gegenstand, was ein Aufprall bewirkt: melden, und bei einem
    /// Bruch genau einmal entfernen (GDD Gegenstandsarten, Kriterium 14; ADR-0001, Entscheidung 5).
    /// </summary>
    /// <remarks>
    /// Normale C#-Klasse ohne Unity-Callback, damit die Regel "einmal melden, einmal entfernen" in
    /// EditMode-Tests prüfbar ist. Melden und Entfernen bekommt sie als Funktionen übergeben; im Spiel
    /// sind das der Meldedienst und die Ausgabestelle, im Test Zähler.
    /// Reihenfolge beim Bruch: erst melden, dann entfernen. NGO stellt Nachrichten in Sendereihenfolge
    /// zu, die Scherben-Meldung kommt also vor dem Entfernen bei den Clients an.
    /// </remarks>
    public sealed class ImpactJudge
    {
        private readonly bool _isFragile;
        private readonly float _breakSpeed;
        private readonly float _mass;
        private readonly Action<ImpactReport> _report;
        private readonly Action _remove;

        /// <summary>Legt die Entscheidung für einen Gegenstand an.</summary>
        /// <param name="isFragile">Die Art ist zerbrechlich.</param>
        /// <param name="breakSpeed">Bruchgeschwindigkeit der Art in m/s.</param>
        /// <param name="mass">Masse des Gegenstands in kg.</param>
        /// <param name="report">Meldet einen Aufprall weiter.</param>
        /// <param name="remove">Entfernt den Gegenstand.</param>
        public ImpactJudge(bool isFragile, float breakSpeed, float mass, Action<ImpactReport> report, Action remove)
        {
            _isFragile = isFragile;
            _breakSpeed = breakSpeed;
            _mass = mass;
            _report = report;
            _remove = remove;
        }

        /// <summary>Ob der Gegenstand schon zerbrochen ist. Danach zählt kein Kontakt mehr.</summary>
        public bool IsBroken { get; private set; }

        /// <summary>
        /// Verarbeitet einen Aufprall. Nach einem Bruch werden weitere Aufpralle ignoriert, auch solche
        /// im selben Physikschritt.
        /// </summary>
        /// <param name="item">Der Gegenstand.</param>
        /// <param name="point">Kontaktpunkt in Weltkoordinaten.</param>
        /// <param name="impactSpeed">Aufprall-Geschwindigkeit in m/s.</param>
        /// <param name="isFalling">Der Gegenstand ist beim Host im Zustand "Fällt".</param>
        /// <param name="other">Was getroffen wurde.</param>
        public void HandleImpact(NetworkObject item, Vector3 point, float impactSpeed, bool isFalling, Collider other)
        {
            if (IsBroken)
            {
                return;
            }

            bool breaks = BreakRule.BreaksOnImpact(_isFragile, isFalling, impactSpeed, _breakSpeed);
            if (breaks)
            {
                // Vor dem Melden sperren: Ein Zuhörer könnte einen weiteren Aufprall auslösen.
                IsBroken = true;
            }

            _report(new ImpactReport(item, point, impactSpeed, _mass, isFalling, breaks, other));

            if (breaks)
            {
                _remove();
            }
        }
    }
}
