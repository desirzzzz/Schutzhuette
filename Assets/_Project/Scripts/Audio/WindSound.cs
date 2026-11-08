using UnityEngine;

namespace Schutzhuette.Audio
{
    /// <summary>
    /// Wind als leise Schleife mit Böen. Steht der Hörer (die Kamera) unter einem Dach,
    /// wird der Wind leiser und dumpfer, wie durch Wände gehört.
    /// </summary>
    /// <remarks>
    /// Netcode: Hier geht nichts übers Netz. Wetter ist noch kein Spielzustand, jeder Rechner
    /// spielt seinen eigenen Wind. Sobald es einen Sturm mit Stärke gibt, liest diese
    /// Komponente dessen Wert (eine NetworkVariable des Hosts) statt fester Werte.
    /// Liegt auf einem Szenen-Objekt, läuft also auch schon im Menü.
    /// </remarks>
    public class WindSound : MonoBehaviour
    {
        [Tooltip("Geräusch-Einstellungen (Clip, Lautstärke drinnen und draußen).")]
        [SerializeField] private SoundSettings _settings;

        private const float OpenCutoff = 22000f;
        private const float CheckInterval = 0.2f;

        private AudioSource _source;
        private AudioLowPassFilter _lowPass;
        private float _indoor;
        private bool _indoorTarget;
        private float _nextCheckTime;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.volume = 0f;

            _lowPass = gameObject.AddComponent<AudioLowPassFilter>();
            _lowPass.cutoffFrequency = OpenCutoff;

            if (_settings != null && _settings.WindLoop != null)
            {
                _source.clip = _settings.WindLoop;
                _source.time = Random.Range(0f, _settings.WindLoop.length);
                _source.Play();
            }
        }

        private void Update()
        {
            if (_settings == null || !_source.isPlaying)
            {
                return;
            }

            // Ein Strahl alle 0,2 s reicht, der Übergang wird ohnehin weich geblendet.
            if (Time.unscaledTime >= _nextCheckTime)
            {
                _nextCheckTime = Time.unscaledTime + CheckInterval;
                Camera listener = Camera.main;
                _indoorTarget = listener != null && Shelter.IsUnderRoof(listener.transform.position, _settings.RoofCheckHeight);
            }

            _indoor = Mathf.MoveTowards(_indoor, _indoorTarget ? 1f : 0f, Time.unscaledDeltaTime / _settings.WindFadeSeconds);

            // Böen: langsames Rauschen um den Grundwert.
            float gust = 1f + _settings.GustStrength * (Mathf.PerlinNoise(Time.time * 0.15f, 0.37f) * 2f - 1f);
            _source.volume = Mathf.Lerp(_settings.WindOutdoorVolume, _settings.WindIndoorVolume, _indoor) * gust;

            // Logarithmisch überblenden, so klingt der Übergang gleichmäßig.
            float logCutoff = Mathf.Lerp(Mathf.Log(OpenCutoff), Mathf.Log(_settings.WindIndoorCutoff), _indoor);
            _lowPass.cutoffFrequency = Mathf.Exp(logCutoff);
        }
    }
}
