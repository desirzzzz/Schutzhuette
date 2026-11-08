using UnityEngine;

namespace Schutzhuette.Hut
{
    /// <summary>
    /// Einstellbare Werte eines Holzofens: Brenndauer, Vorrat und wie schnell die Wärme
    /// steigt oder sinkt. Als ScriptableObject, damit man sie ohne Code ändern kann und
    /// später verschiedene Öfen (z. B. ein größerer Ofen als Upgrade) eigene Werte bekommen.
    /// </summary>
    [CreateAssetMenu(fileName = "StoveSettings", menuName = "Schutzhütte/Ofen-Einstellungen")]
    public class StoveSettings : ScriptableObject
    {
        [Tooltip("So viele Sekunden Brennstoff hat der Ofen beim Start einer Sitzung.")]
        [SerializeField, Min(0f)] private float _startFuelSeconds = 60f;

        [Tooltip("Mehr Brennstoff als so viele Sekunden passt nicht in den Ofen.")]
        [SerializeField, Min(1f)] private float _maxFuelSeconds = 240f;

        [Tooltip("Höchste Wärme (Anzeige in Prozent davon).")]
        [SerializeField, Min(1f)] private float _maxHeat = 100f;

        [Tooltip("Um so viel steigt die Wärme pro Sekunde, solange der Ofen brennt.")]
        [SerializeField, Min(0f)] private float _heatRisePerSecond = 4f;

        [Tooltip("Um so viel sinkt die Wärme pro Sekunde, wenn der Ofen aus ist.")]
        [SerializeField, Min(0f)] private float _heatDecayPerSecond = 1.5f;

        /// <summary>Brennstoff beim Start, in Sekunden.</summary>
        public float StartFuelSeconds => _startFuelSeconds;

        /// <summary>Höchster Brennstoffvorrat, in Sekunden.</summary>
        public float MaxFuelSeconds => _maxFuelSeconds;

        /// <summary>Höchste Wärme.</summary>
        public float MaxHeat => _maxHeat;

        /// <summary>Wärmeanstieg pro Sekunde beim Brennen.</summary>
        public float HeatRisePerSecond => _heatRisePerSecond;

        /// <summary>Wärmeverlust pro Sekunde, wenn der Ofen aus ist.</summary>
        public float HeatDecayPerSecond => _heatDecayPerSecond;
    }
}
