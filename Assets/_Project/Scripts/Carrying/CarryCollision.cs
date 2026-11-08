using System;
using Schutzhuette.Player;
using UnityEngine;

namespace Schutzhuette.Carrying
{
    /// <summary>
    /// Prüft, ob ein getragener Gegenstand an einer gedachten Stelle in feste Umgebung
    /// (Wände, Türrahmen, Möbel) ragen würde, und kürzt Bewegungen entsprechend.
    /// </summary>
    /// <remarks>
    /// Warum so: Getragene Gegenstände folgen dem Träger und sind dabei kinematisch oder
    /// ohne Kollision (Regel 3). Physik kann sie also nicht aufhalten. Stattdessen prüft der
    /// Spieler, dem die Figur gehört, vor jedem Schritt, ob der Gegenstand danach in einer
    /// Wand stecken würde, und läuft dann eben nicht dorthin.
    /// Reine Funktionen ohne Zustand.
    /// </remarks>
    public static class CarryCollision
    {
        // Etwas kleiner prüfen, damit bloßes Berühren (z. B. Kante auf Kante) nicht blockiert.
        private const float ShrinkFactor = 0.95f;

        /// <summary>
        /// Würde ein Quader mit dieser Lage feste Umgebung überlappen?
        /// </summary>
        /// <param name="center">Mitte des Quaders in Weltkoordinaten.</param>
        /// <param name="halfExtents">Halbe Kantenlängen in Metern.</param>
        /// <param name="rotation">Drehung des Quaders.</param>
        public static bool HitsEnvironment(Vector3 center, Vector3 halfExtents, Quaternion rotation)
        {
            Collider[] hits = Physics.OverlapBox(center, halfExtents * ShrinkFactor, rotation,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            foreach (Collider hit in hits)
            {
                if (IsEnvironment(hit))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Feste Umgebung: Collider ohne Rigidbody, die zu keiner Spielfigur gehören.
        /// Kisten, die Truhe (haben Rigidbodys) und Mitspieler zählen nicht dazu.
        /// </summary>
        public static bool IsEnvironment(Collider collider)
        {
            return collider.attachedRigidbody == null
                && !(collider is CharacterController)
                && collider.GetComponentInParent<PlayerMovement>() == null;
        }

        /// <summary>
        /// Erst die ganze Verschiebung versuchen, dann nur den x- oder nur den z-Anteil.
        /// So gleitet man an einer Wand entlang, statt ganz stehen zu bleiben.
        /// </summary>
        /// <param name="delta">Gewünschte waagrechte Verschiebung.</param>
        /// <param name="isBlocked">Sagt für eine Verschiebung, ob sie in eine Wand führt.</param>
        public static Vector3 Slide(Vector3 delta, Func<Vector3, bool> isBlocked)
        {
            if (!isBlocked(delta))
            {
                return delta;
            }

            var alongX = new Vector3(delta.x, 0f, 0f);
            if (alongX.sqrMagnitude > 0f && !isBlocked(alongX))
            {
                return alongX;
            }

            var alongZ = new Vector3(0f, 0f, delta.z);
            if (alongZ.sqrMagnitude > 0f && !isBlocked(alongZ))
            {
                return alongZ;
            }

            return Vector3.zero;
        }
    }
}
