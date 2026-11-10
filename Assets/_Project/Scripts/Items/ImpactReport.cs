using Unity.Netcode;
using UnityEngine;

namespace Schutzhuette.Items
{
    /// <summary>
    /// Ein Aufprall, wie ihn der Host gemessen hat. Gibt es nur beim Host, geht nicht ins Netz
    /// (ADR-0001, Entscheidung 4).
    /// </summary>
    /// <remarks>
    /// Alles, was ein Zuhörer braucht, steht hier drin. Das Collision-Objekt von Unity darf niemand
    /// über den Callback hinaus behalten, weil Unity es wiederverwendet.
    /// </remarks>
    public readonly struct ImpactReport
    {
        /// <summary>Der aufprallende Gegenstand.</summary>
        public readonly NetworkObject Item;

        /// <summary>Kontaktpunkt mit dem größten senkrechten Anteil, in Weltkoordinaten.</summary>
        public readonly Vector3 Point;

        /// <summary>Aufprall-Geschwindigkeit in m/s: größter senkrechter Anteil über alle Kontaktpunkte.</summary>
        public readonly float ImpactSpeed;

        /// <summary>Masse des Gegenstands in kg, aus dem Katalog.</summary>
        public readonly float Mass;

        /// <summary>Ob der Gegenstand beim Host im Zustand "Fällt" war.</summary>
        public readonly bool WasFalling;

        /// <summary>Ob die Bruchregel ausgelöst hat. Der Gegenstand wird danach entfernt.</summary>
        public readonly bool Broke;

        /// <summary>Was getroffen wurde, zum Beispiel eine Wand oder ein Gast.</summary>
        public readonly Collider Other;

        /// <summary>Legt eine Aufprall-Meldung an. Nur der Aufprall-Messer des Hosts tut das.</summary>
        public ImpactReport(NetworkObject item, Vector3 point, float impactSpeed, float mass, bool wasFalling,
            bool broke, Collider other)
        {
            Item = item;
            Point = point;
            ImpactSpeed = impactSpeed;
            Mass = mass;
            WasFalling = wasFalling;
            Broke = broke;
            Other = other;
        }
    }
}
