using Schutzhuette.Carrying;
using Unity.Netcode;
using UnityEngine;

namespace Schutzhuette.Audio
{
    /// <summary>
    /// Lässt einen Gegenstand (Kiste, Truhe, Holzscheit) hörbar aufprallen, bei allen Spielern.
    /// Je schwerer und je härter, desto lauter und tiefer (siehe <see cref="ImpactAudio"/>).
    /// </summary>
    /// <remarks>
    /// Netcode: Die Physik der Gegenstände rechnet nur der Host (Architektur-Regel 3), also
    /// bekommt auch nur er die Kollision gemeldet (OnCollisionEnter). Er schickt Ort und
    /// Aufprallgeschwindigkeit per RPC an alle, auch an sich selbst. Ein RPC statt einer
    /// NetworkVariable, weil ein Geräusch ein einmaliges Ereignis ist und kein Zustand:
    /// Wer später beitritt, muss vergangene Aufpralle nicht hören.
    ///
    /// Vorhersage: Wirft oder legt ein Client eine Kiste ab, fliegt bei ihm schon eine
    /// Vorhersage-Kopie (siehe PlayerCarry). Die Kopie macht ihre Aufpralle selbst
    /// (<see cref="AttachToGhost"/>), sonst käme der Ton bei ihm um den Ping zu spät.
    /// Solange die echte Kiste bei ihm ausgeblendet ist, überspringt er deshalb die RPC.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody))]
    public class ImpactSound : NetworkBehaviour
    {
        [Tooltip("Geräusch-Einstellungen (Clips, Lautstärken).")]
        [SerializeField] private SoundSettings _settings;

        private Rigidbody _rigidbody;
        private Carryable _carryable;
        private float _nextAllowedTime;

        /// <summary>Gewicht in kg (aus dem Rigidbody).</summary>
        public float Mass => _rigidbody != null ? _rigidbody.mass : 0f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _carryable = GetComponent<Carryable>();
        }

        // Nur auf dem Host: Bei den Clients ist der Rigidbody kinematisch und folgt dem Netz.
        private void OnCollisionEnter(Collision collision)
        {
            if (!IsServer || !IsSpawned || _settings == null)
            {
                return;
            }

            if (!ImpactAudio.TryGetImpact(collision, out Vector3 point, out float speed)
                || speed < _settings.MinImpactSpeed
                || Time.time < _nextAllowedTime)
            {
                return;
            }

            // Stoßen zwei solche Gegenstände zusammen, melden beide die Kollision.
            // Nur einer soll klingen: der schwerere (bei gleichem Gewicht der mit der kleineren ID).
            if (collision.rigidbody != null
                && collision.rigidbody.TryGetComponent(out ImpactSound other)
                && other != this
                && other.TakesPrecedenceOver(this))
            {
                return;
            }

            _nextAllowedTime = Time.time + _settings.ImpactCooldown;
            PlayImpactRpc(point, speed);
        }

        private bool TakesPrecedenceOver(ImpactSound other)
        {
            if (!Mathf.Approximately(Mass, other.Mass))
            {
                return Mass > other.Mass;
            }

            return NetworkObjectId < other.NetworkObjectId;
        }

        // Läuft auf allen Rechnern (Clients und Host), aufgerufen vom Host.
        [Rpc(SendTo.ClientsAndHost)]
        private void PlayImpactRpc(Vector3 point, float speed)
        {
            // Hier fliegt gerade eine Vorhersage-Kopie an Stelle der echten Kiste: Die hat den
            // Aufprall schon selbst hörbar gemacht.
            if (_carryable != null && _carryable.IsLocallyHidden)
            {
                return;
            }

            ImpactAudio.Play(_settings, Mass, point, speed);
        }

        /// <summary>
        /// Gibt einer lokalen Vorhersage-Kopie (mit Rigidbody und Collider) dieses Gegenstands
        /// eigene Aufprall-Geräusche. Nur lokal, geht nicht ins Netz.
        /// </summary>
        public void AttachToGhost(GameObject ghost)
        {
            ghost.AddComponent<GhostImpactSound>().Init(_settings, Mass);
        }
    }
}
