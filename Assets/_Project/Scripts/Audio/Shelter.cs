using Schutzhuette.Carrying;
using UnityEngine;

namespace Schutzhuette.Audio
{
    /// <summary>
    /// Beantwortet die Frage "Ist man hier drinnen?" mit einem Strahl nach oben:
    /// Trifft er ein Dach (feste Umgebung), ist man unter Dach.
    /// </summary>
    /// <remarks>
    /// Warum so: Es braucht keine eigene Zone in der Szene und gilt später für jedes
    /// Gebäude mit Dach von selbst. Kisten, Truhe und Spielfiguren zählen nicht als Dach
    /// (gleiche Regel wie <see cref="CarryCollision.IsEnvironment"/>). Reine Funktion ohne
    /// Zustand, jeder Rechner fragt für sich.
    /// </remarks>
    public static class Shelter
    {
        /// <summary>Liegt über dieser Stelle (bis zur angegebenen Höhe) ein Dach?</summary>
        /// <param name="position">Startpunkt, z. B. Körpermitte einer Figur oder die Kamera.</param>
        /// <param name="maxHeight">So weit nach oben wird gesucht, in Metern.</param>
        public static bool IsUnderRoof(Vector3 position, float maxHeight)
        {
            RaycastHit[] hits = Physics.RaycastAll(position, Vector3.up, maxHeight,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                if (CarryCollision.IsEnvironment(hit.collider))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
