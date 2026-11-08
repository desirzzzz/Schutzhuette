using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Schutzhuette.Carrying
{
    /// <summary>
    /// Ein Gegenstand, den Spieler aufheben, tragen, ablegen und werfen können.
    /// Er gehört immer dem Host (Server-Autorität).
    /// </summary>
    /// <remarks>
    /// Zustände:
    /// liegt (der Host rechnet die Physik, in Ruhe schläft der Rigidbody ein) →
    /// getragen (Kind der Spielfigur, kinematisch, Kollision aus) →
    /// abgelegt oder geworfen (wieder Physik beim Host).
    /// "Getragen" ist kein eigener Netzwerk-Wert: NGO überträgt das Eltern-Objekt
    /// an alle Rechner, daraus ergibt sich der Zustand überall von selbst.
    /// Erwartet, dass die Kiste ohne Eltern-Objekt in der Szene liegt.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody), typeof(NetworkRigidbody))]
    public class Carryable : NetworkBehaviour
    {
        private Rigidbody _rigidbody;
        private NetworkRigidbody _networkRigidbody;
        private Collider _collider;

        /// <summary>Wird der Gegenstand gerade getragen? Stimmt auf Host und Clients.</summary>
        public bool IsCarried => transform.parent != null;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _networkRigidbody = GetComponent<NetworkRigidbody>();
            _collider = GetComponent<Collider>();
        }

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            // Wer später beitritt, bekommt die Kiste eventuell schon getragen.
            _collider.enabled = !IsCarried;
        }

        /// <summary>
        /// Nur auf dem Host: hängt den Gegenstand an die Spielfigur des Trägers.
        /// </summary>
        /// <param name="carrier">NetworkObject der tragenden Spielfigur.</param>
        /// <param name="holdLocalPosition">Haltepunkt relativ zur Spielfigur.</param>
        /// <returns>true, wenn es geklappt hat.</returns>
        public bool AttachTo(NetworkObject carrier, Vector3 holdLocalPosition)
        {
            if (!IsServer)
            {
                return false;
            }

            // Erst Schwung wegnehmen, dann kinematisch machen. Ein kinematischer
            // Körper folgt nur noch seinem Eltern-Objekt und rechnet keine Physik.
            if (!_rigidbody.isKinematic)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            _networkRigidbody.SetIsKinematic(true);

            if (!NetworkObject.TrySetParent(carrier, false))
            {
                _networkRigidbody.SetIsKinematic(false);
                return false;
            }

            transform.SetLocalPositionAndRotation(holdLocalPosition, Quaternion.identity);
            return true;
        }

        /// <summary>
        /// Nur auf dem Host: löst den Gegenstand vom Träger und übergibt ihn der Physik.
        /// </summary>
        /// <param name="position">Startposition in Weltkoordinaten.</param>
        /// <param name="rotation">Startdrehung in Weltkoordinaten.</param>
        /// <param name="velocity">Startgeschwindigkeit, Vector3.zero zum Ablegen.</param>
        public void Detach(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            if (!IsServer)
            {
                return;
            }

            NetworkObject.TryRemoveParent(true);
            transform.SetPositionAndRotation(position, rotation);

            _collider.enabled = true;
            _networkRigidbody.SetIsKinematic(false);
            _rigidbody.linearVelocity = velocity;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.WakeUp();
        }

        /// <inheritdoc />
        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            bool carried = parentNetworkObject != null;

            // Getragen: Kollision aus, sonst blockiert die Kiste ihren eigenen Träger.
            _collider.enabled = !carried;

            // Trennt sich der Träger, löst NGO die Kiste selbst von ihm.
            // Dann soll sie beim Host herunterfallen statt in der Luft zu hängen.
            if (IsServer && !carried && _rigidbody.isKinematic)
            {
                _networkRigidbody.SetIsKinematic(false);
                _rigidbody.WakeUp();
            }
        }
    }
}
