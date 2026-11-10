using UnityEngine;

namespace Schutzhuette.Items
{
    /// <summary>Ergebnis der Prüfung einer Bruchgeschwindigkeit gegen das Prüffenster.</summary>
    public enum BreakSpeedCheck
    {
        /// <summary>Die Bruchgeschwindigkeit liegt im Fenster.</summary>
        Ok = 0,

        /// <summary>Zu niedrig: Schon das Ablegen aus Haltehöhe zerbricht den Gegenstand.</summary>
        BreaksWhenPutDown = 1,

        /// <summary>Zu hoch: Ein gerader Wurf gegen die Wand zerbricht ihn nicht mehr.</summary>
        StraightThrowsDoNotBreak = 2,
    }

    /// <summary>
    /// Rechenregeln für das Zerbrechen (GDD Gegenstandsarten, Formel 2; ADR-0001, Entscheidung 5).
    /// Reine Funktionen: Sie bekommen alle Werte übergeben und laufen deshalb ohne Szene in Tests.
    /// </summary>
    /// <remarks>
    /// Nur der Host wendet die Bruchregel auf echte Aufpralle an (im Messpunkt für Aufpralle).
    /// Vorhersage-Kopien beim Client zerbrechen nie selbst.
    /// </remarks>
    /// <example>
    /// <code>
    /// float speed = BreakRule.ImpactSpeed(collision.relativeVelocity, contact.normal);
    /// bool breaks = BreakRule.BreaksOnImpact(kind.Fragile.Enabled, isFalling, speed, kind.Fragile.BreakSpeed);
    /// </code>
    /// </example>
    public static class BreakRule
    {
        /// <summary>
        /// Schwerkraft des Projekts in m/s² (Project Settings → Physics). Als Konstante, damit die
        /// Katalogprüfung ohne Szene rechnet. Ändert sich die Schwerkraft dort, muss dieser Wert mit.
        /// </summary>
        public const float ProjectGravity = 9.81f;

        // Prüffenster aus dem GDD: 1,1 × Fallgeschwindigkeit ≤ Bruchgeschwindigkeit ≤ 0,9 × Wurftempo.
        private const float PutDownMargin = 1.1f;
        private const float ThrowMargin = 0.9f;

        // Ein Wert genau auf der Grenze des Fensters soll nicht an Rundung scheitern.
        private const float WindowTolerance = 0.0001f;

        /// <summary>
        /// Aufprall-Geschwindigkeit in m/s: der Anteil der Relativgeschwindigkeit senkrecht zur Fläche.
        /// Ein Wurf, der die Wand nur streift, zählt deshalb kaum.
        /// </summary>
        /// <param name="relativeVelocity">Relative Geschwindigkeit der beiden Kontaktpartner.</param>
        /// <param name="contactNormal">Kontaktnormale, Länge 1.</param>
        public static float ImpactSpeed(Vector3 relativeVelocity, Vector3 contactNormal)
        {
            return Mathf.Abs(Vector3.Dot(relativeVelocity, contactNormal));
        }

        /// <summary>
        /// Ob ein Aufprall den Gegenstand zerbricht: nur Zerbrechliches, nur im Zustand "Fällt",
        /// und nur, wenn der Aufprall schneller ist als die Bruchgeschwindigkeit.
        /// </summary>
        /// <param name="isFragile">Die Art hat die Eigenschaft "zerbrechlich".</param>
        /// <param name="isFalling">Der Gegenstand ist beim Host im Zustand "Fällt". Getragenes zerbricht nie.</param>
        /// <param name="impactSpeed">Aufprall-Geschwindigkeit aus <see cref="ImpactSpeed"/>.</param>
        /// <param name="breakSpeed">Bruchgeschwindigkeit der Art in m/s.</param>
        public static bool BreaksOnImpact(bool isFragile, bool isFalling, float impactSpeed, float breakSpeed)
        {
            return isFragile && isFalling && impactSpeed > breakSpeed;
        }

        /// <summary>
        /// Geschwindigkeit in m/s, mit der ein losgelassener Gegenstand auf einer Fläche aufkommt.
        /// Liegt die Fläche über der Haltehöhe, ist das Ergebnis 0.
        /// </summary>
        /// <param name="holdHeight">Höhe des Haltepunkts über dem Boden in m.</param>
        /// <param name="surfaceHeight">Höhe der Fläche über dem Boden in m (Boden 0, Tisch etwa 0,9).</param>
        /// <param name="gravity">Schwerkraft als positiver Betrag in m/s².</param>
        public static float ReleaseFallSpeed(float holdHeight, float surfaceHeight, float gravity)
        {
            float drop = Mathf.Max(0f, holdHeight - surfaceHeight);
            return Mathf.Sqrt(Mathf.Max(0f, 2f * gravity * drop));
        }

        /// <summary>Untere Grenze des Prüffensters: Darunter zerbricht schon das Ablegen auf den Boden.</summary>
        public static float MinBreakSpeed(float holdHeight, float gravity)
        {
            return PutDownMargin * ReleaseFallSpeed(holdHeight, 0f, gravity);
        }

        /// <summary>Obere Grenze des Prüffensters: Darüber zerbricht ein gerader Wurf gegen die Wand nicht.</summary>
        public static float MaxBreakSpeed(float throwSpeed)
        {
            return ThrowMargin * throwSpeed;
        }

        /// <summary>
        /// Prüft eine Bruchgeschwindigkeit gegen das Fenster aus Haltehöhe und Wurftempo.
        /// Das Ergebnis ist nur ein Hinweis für den Editor, nie ein Fehler.
        /// </summary>
        /// <param name="breakSpeed">Bruchgeschwindigkeit der Art in m/s.</param>
        /// <param name="holdHeight">Prüfwert Haltehöhe in m (Katalog-Einstellungen).</param>
        /// <param name="throwSpeed">Prüfwert Wurftempo in m/s (Katalog-Einstellungen).</param>
        /// <param name="gravity">Schwerkraft als positiver Betrag in m/s².</param>
        public static BreakSpeedCheck CheckBreakSpeed(float breakSpeed, float holdHeight, float throwSpeed,
            float gravity)
        {
            if (breakSpeed < MinBreakSpeed(holdHeight, gravity) - WindowTolerance)
            {
                return BreakSpeedCheck.BreaksWhenPutDown;
            }

            if (breakSpeed > MaxBreakSpeed(throwSpeed) + WindowTolerance)
            {
                return BreakSpeedCheck.StraightThrowsDoNotBreak;
            }

            return BreakSpeedCheck.Ok;
        }
    }
}
