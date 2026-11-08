using UnityEngine;

namespace Schutzhuette.Player
{
    /// <summary>
    /// Für Komponenten auf der Spielfigur, die das Laufen und Drehen verlangsamen oder
    /// einschränken, zum Beispiel beim Tragen. <see cref="PlayerMovement"/> und
    /// <see cref="PlayerLook"/> fragen jedes Frame alle diese Komponenten ab.
    /// </summary>
    /// <remarks>
    /// Läuft nur auf dem Rechner, dem die Figur gehört (Owner-Autorität der Bewegung).
    /// </remarks>
    public interface IMovementModifier
    {
        /// <summary>Faktor für die Laufgeschwindigkeit, 1 = normal.</summary>
        float SpeedMultiplier { get; }

        /// <summary>
        /// Passt die gewünschte waagrechte Bewegung dieses Frames an.
        /// </summary>
        /// <param name="currentPosition">Aktuelle Weltposition der Figur.</param>
        /// <param name="desiredDelta">Gewünschte Verschiebung in Metern (Weltkoordinaten, ohne Schwerkraft).</param>
        /// <returns>Die erlaubte Verschiebung.</returns>
        Vector3 ConstrainMove(Vector3 currentPosition, Vector3 desiredDelta);

        /// <summary>
        /// Passt die gewünschte Drehung um die Hochachse in diesem Frame an.
        /// </summary>
        /// <param name="currentPosition">Aktuelle Weltposition der Figur.</param>
        /// <param name="currentRotation">Aktuelle Drehung der Figur.</param>
        /// <param name="desiredYawDegrees">Gewünschte Drehung in Grad.</param>
        /// <returns>Die erlaubte Drehung in Grad.</returns>
        float ConstrainYaw(Vector3 currentPosition, Quaternion currentRotation, float desiredYawDegrees);
    }
}
