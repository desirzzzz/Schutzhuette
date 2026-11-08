using UnityEngine;

namespace Schutzhuette.Audio
{
    /// <summary>
    /// Aufprall-Geräusche einer lokalen Vorhersage-Kopie. Wird von
    /// <see cref="ImpactSound.AttachToGhost"/> zur Laufzeit angehängt.
    /// </summary>
    /// <remarks>Nur lokal: Die Kopie gibt es nur auf dem Rechner, der sie vorhersagt.</remarks>
    public class GhostImpactSound : MonoBehaviour
    {
        private SoundSettings _settings;
        private float _mass;
        private float _nextAllowedTime;

        /// <summary>Einstellungen und Gewicht des echten Gegenstands übernehmen.</summary>
        public void Init(SoundSettings settings, float mass)
        {
            _settings = settings;
            _mass = mass;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_settings == null
                || Time.time < _nextAllowedTime
                || !ImpactAudio.TryGetImpact(collision, out Vector3 point, out float speed)
                || speed < _settings.MinImpactSpeed)
            {
                return;
            }

            _nextAllowedTime = Time.time + _settings.ImpactCooldown;
            ImpactAudio.Play(_settings, _mass, point, speed);
        }
    }
}
