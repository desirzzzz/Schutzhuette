using Schutzhuette.Hut;
using UnityEngine;

namespace Schutzhuette.Audio
{
    /// <summary>
    /// Knistern des Ofens als Schleife: blendet ein, wenn er brennt, und aus, wenn er ausgeht.
    /// </summary>
    /// <remarks>
    /// Netcode: Hier geht nichts übers Netz. Ob der Ofen brennt, steht in der NetworkVariable
    /// des <see cref="Stove"/> (nur der Host ändert sie). Jeder Rechner liest sie und spielt
    /// das Knistern selbst.
    /// </remarks>
    [RequireComponent(typeof(Stove))]
    public class StoveSound : MonoBehaviour
    {
        [Tooltip("Geräusch-Einstellungen (Clip, Lautstärke, Hörweite).")]
        [SerializeField] private SoundSettings _settings;

        private Stove _stove;
        private AudioSource _source;

        private void Awake()
        {
            _stove = GetComponent<Stove>();

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.volume = 0f;
            _source.spatialBlend = 1f;
            // Linear: Ab der Hörweite ist es wirklich still (draußen hört man den Ofen nicht).
            _source.rolloffMode = AudioRolloffMode.Linear;
            _source.minDistance = 1.5f;
            _source.dopplerLevel = 0f;

            if (_settings != null)
            {
                _source.clip = _settings.StoveLoop;
                _source.maxDistance = _settings.StoveMaxDistance;
            }
        }

        private void Update()
        {
            if (_settings == null || _source.clip == null)
            {
                return;
            }

            float target = _stove.IsBurning ? _settings.StoveVolume : 0f;
            float step = _settings.StoveVolume / _settings.StoveFadeSeconds * Time.deltaTime;
            _source.volume = Mathf.MoveTowards(_source.volume, target, step);

            if (_source.volume > 0f && !_source.isPlaying)
            {
                // Zufällige Startstelle, damit es nicht jedes Mal gleich anfängt.
                _source.time = Random.Range(0f, _source.clip.length);
                _source.Play();
            }
            else if (_source.volume <= 0f && _source.isPlaying)
            {
                _source.Stop();
            }
        }
    }
}
