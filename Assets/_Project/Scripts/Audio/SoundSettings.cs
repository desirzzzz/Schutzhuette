using UnityEngine;

namespace Schutzhuette.Audio
{
    /// <summary>
    /// Alle Geräusche des Prototyps an einem Ort: welche Clips, wie laut, wie weit hörbar.
    /// Als ScriptableObject, damit man den Klang ohne Code einstellen kann.
    /// </summary>
    /// <remarks>
    /// Das Editor-Skript "Schutzhütte/Geräusche einrichten" trägt die Clips aus
    /// Assets/_Project/Art/Audio ein. Die Zahlen hier sind Startwerte zum Nachstimmen.
    /// </remarks>
    [CreateAssetMenu(fileName = "SoundSettings", menuName = "Schutzhütte/Geräusch-Einstellungen")]
    public class SoundSettings : ScriptableObject
    {
        [Header("Aufprall")]
        [Tooltip("Holz-Aufpralle, einer davon wird zufällig gespielt.")]
        [SerializeField] private AudioClip[] _impactClips = new AudioClip[0];

        [Tooltip("Dumpfe Schicht unter dem Holz-Aufprall. Je schwerer der Gegenstand, desto lauter.")]
        [SerializeField] private AudioClip[] _heavyImpactClips = new AudioClip[0];

        [Tooltip("Gesamtlautstärke der Aufpralle (0 bis 1).")]
        [SerializeField, Range(0f, 1f)] private float _impactVolume = 1f;

        [Tooltip("Langsamere Aufpralle (m/s, senkrecht zur Fläche) machen kein Geräusch.")]
        [SerializeField, Min(0f)] private float _minImpactSpeed = 0.6f;

        [Tooltip("Ab dieser Aufprallgeschwindigkeit (m/s) ist es am lautesten.")]
        [SerializeField, Min(0.1f)] private float _fullImpactSpeed = 6f;

        [Tooltip("Gewicht des leichtesten Gegenstands (kg): klingt am hellsten.")]
        [SerializeField, Min(0.1f)] private float _lightMass = 3f;

        [Tooltip("Gewicht des schwersten Gegenstands (kg): klingt am tiefsten und dumpfsten.")]
        [SerializeField, Min(0.1f)] private float _heavyMass = 80f;

        [Tooltip("Tonhöhe bei leichtem und bei schwerem Gegenstand (1 = Original).")]
        [SerializeField, Range(0.3f, 2f)] private float _lightPitch = 1.15f;
        [SerializeField, Range(0.3f, 2f)] private float _heavyPitch = 0.6f;

        [Tooltip("Tiefpass bei leichtem und bei schwerem Gegenstand (Hz). Kleiner = dumpfer.")]
        [SerializeField, Min(100f)] private float _lightCutoff = 9000f;
        [SerializeField, Min(100f)] private float _heavyCutoff = 1400f;

        [Tooltip("Mindestabstand zwischen zwei Aufprall-Geräuschen desselben Gegenstands (s).")]
        [SerializeField, Min(0f)] private float _impactCooldown = 0.12f;

        [Tooltip("Bis zu dieser Entfernung (m) ist ein Aufprall gut hörbar.")]
        [SerializeField, Min(1f)] private float _impactMaxDistance = 30f;

        [Header("Schritte")]
        [Tooltip("Schritte auf Holz (in der Hütte).")]
        [SerializeField] private AudioClip[] _woodSteps = new AudioClip[0];

        [Tooltip("Schritte im Schnee (draußen).")]
        [SerializeField] private AudioClip[] _snowSteps = new AudioClip[0];

        [Tooltip("Lautstärke ohne Last und mit voller Last.")]
        [SerializeField, Range(0f, 1f)] private float _stepVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _loadedStepVolume = 0.75f;

        [Tooltip("Tonhöhe ohne Last und mit voller Last (kleiner = schwerer).")]
        [SerializeField, Range(0.3f, 2f)] private float _stepPitch = 1f;
        [SerializeField, Range(0.3f, 2f)] private float _loadedStepPitch = 0.78f;

        [Tooltip("Meter pro Schritt ohne Last und mit voller Last.")]
        [SerializeField, Min(0.2f)] private float _stepLength = 1.1f;
        [SerializeField, Min(0.2f)] private float _loadedStepLength = 1.3f;

        [Tooltip("Ab dieser Last pro Träger (kg) klingen die Schritte voll beladen. Die Truhe teilen sich zwei.")]
        [SerializeField, Min(0.1f)] private float _fullLoadMass = 40f;

        [Tooltip("Bis zu dieser Entfernung (m) hört man Schritte.")]
        [SerializeField, Min(1f)] private float _stepMaxDistance = 20f;

        [Header("Ofen")]
        [Tooltip("Knistern als Schleife, läuft nur, solange der Ofen brennt.")]
        [SerializeField] private AudioClip _stoveLoop;

        [SerializeField, Range(0f, 1f)] private float _stoveVolume = 0.8f;

        [Tooltip("Weiter weg (m) hört man den Ofen nicht mehr.")]
        [SerializeField, Min(1f)] private float _stoveMaxDistance = 12f;

        [Tooltip("So lange (s) dauert das Ein- und Ausblenden beim Anzünden und Ausgehen.")]
        [SerializeField, Min(0.01f)] private float _stoveFadeSeconds = 1.5f;

        [Header("Wind")]
        [Tooltip("Wind als Schleife, überall zu hören.")]
        [SerializeField] private AudioClip _windLoop;

        [Tooltip("Lautstärke draußen und unter dem Dach.")]
        [SerializeField, Range(0f, 1f)] private float _windOutdoorVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _windIndoorVolume = 0.12f;

        [Tooltip("Tiefpass unter dem Dach (Hz): der Wind klingt gedämpft durch die Wände.")]
        [SerializeField, Min(100f)] private float _windIndoorCutoff = 700f;

        [Tooltip("So lange (s) dauert der Übergang zwischen drinnen und draußen.")]
        [SerializeField, Min(0.01f)] private float _windFadeSeconds = 0.6f;

        [Tooltip("Wie stark Böen die Lautstärke schwanken lassen (0 = gar nicht).")]
        [SerializeField, Range(0f, 1f)] private float _gustStrength = 0.3f;

        [Header("Drinnen oder draußen")]
        [Tooltip("So weit (m) wird nach oben nach einem Dach gesucht.")]
        [SerializeField, Min(0.5f)] private float _roofCheckHeight = 8f;

        /// <summary>Holz-Aufpralle.</summary>
        public AudioClip[] ImpactClips => _impactClips;

        /// <summary>Dumpfe Schicht für schwere Gegenstände.</summary>
        public AudioClip[] HeavyImpactClips => _heavyImpactClips;

        /// <summary>Gesamtlautstärke der Aufpralle.</summary>
        public float ImpactVolume => _impactVolume;

        /// <summary>Unter dieser Aufprallgeschwindigkeit (m/s) bleibt es still.</summary>
        public float MinImpactSpeed => _minImpactSpeed;

        /// <summary>Ab dieser Aufprallgeschwindigkeit (m/s) ist es am lautesten.</summary>
        public float FullImpactSpeed => _fullImpactSpeed;

        /// <summary>Leichtestes Gewicht (kg) für die Klangfarbe.</summary>
        public float LightMass => _lightMass;

        /// <summary>Schwerstes Gewicht (kg) für die Klangfarbe.</summary>
        public float HeavyMass => _heavyMass;

        /// <summary>Tonhöhe bei leichtem Gegenstand.</summary>
        public float LightPitch => _lightPitch;

        /// <summary>Tonhöhe bei schwerem Gegenstand.</summary>
        public float HeavyPitch => _heavyPitch;

        /// <summary>Tiefpass bei leichtem Gegenstand (Hz).</summary>
        public float LightCutoff => _lightCutoff;

        /// <summary>Tiefpass bei schwerem Gegenstand (Hz).</summary>
        public float HeavyCutoff => _heavyCutoff;

        /// <summary>Mindestabstand zwischen zwei Aufprallen desselben Gegenstands (s).</summary>
        public float ImpactCooldown => _impactCooldown;

        /// <summary>Hörweite eines Aufpralls (m).</summary>
        public float ImpactMaxDistance => _impactMaxDistance;

        /// <summary>Schritte auf Holz.</summary>
        public AudioClip[] WoodSteps => _woodSteps;

        /// <summary>Schritte im Schnee.</summary>
        public AudioClip[] SnowSteps => _snowSteps;

        /// <summary>Schritt-Lautstärke ohne Last.</summary>
        public float StepVolume => _stepVolume;

        /// <summary>Schritt-Lautstärke mit voller Last.</summary>
        public float LoadedStepVolume => _loadedStepVolume;

        /// <summary>Schritt-Tonhöhe ohne Last.</summary>
        public float StepPitch => _stepPitch;

        /// <summary>Schritt-Tonhöhe mit voller Last.</summary>
        public float LoadedStepPitch => _loadedStepPitch;

        /// <summary>Meter pro Schritt ohne Last.</summary>
        public float StepLength => _stepLength;

        /// <summary>Meter pro Schritt mit voller Last.</summary>
        public float LoadedStepLength => _loadedStepLength;

        /// <summary>Last pro Träger (kg), ab der die Schritte voll beladen klingen.</summary>
        public float FullLoadMass => _fullLoadMass;

        /// <summary>Hörweite der Schritte (m).</summary>
        public float StepMaxDistance => _stepMaxDistance;

        /// <summary>Knister-Schleife des Ofens.</summary>
        public AudioClip StoveLoop => _stoveLoop;

        /// <summary>Lautstärke des brennenden Ofens.</summary>
        public float StoveVolume => _stoveVolume;

        /// <summary>Hörweite des Ofens (m).</summary>
        public float StoveMaxDistance => _stoveMaxDistance;

        /// <summary>Ein- und Ausblendzeit des Ofens (s).</summary>
        public float StoveFadeSeconds => _stoveFadeSeconds;

        /// <summary>Wind-Schleife.</summary>
        public AudioClip WindLoop => _windLoop;

        /// <summary>Wind-Lautstärke draußen.</summary>
        public float WindOutdoorVolume => _windOutdoorVolume;

        /// <summary>Wind-Lautstärke unter dem Dach.</summary>
        public float WindIndoorVolume => _windIndoorVolume;

        /// <summary>Tiefpass für den Wind unter dem Dach (Hz).</summary>
        public float WindIndoorCutoff => _windIndoorCutoff;

        /// <summary>Übergangszeit drinnen/draußen (s).</summary>
        public float WindFadeSeconds => _windFadeSeconds;

        /// <summary>Stärke der Böen.</summary>
        public float GustStrength => _gustStrength;

        /// <summary>Suchhöhe für das Dach (m).</summary>
        public float RoofCheckHeight => _roofCheckHeight;
    }
}
