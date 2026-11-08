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
        private Renderer[] _renderers;

        /// <summary>Wird der Gegenstand gerade getragen? Stimmt auf Host und Clients.</summary>
        public bool IsCarried => transform.parent != null;

        /// <summary>
        /// Ist die echte Kiste auf diesem Rechner gerade ausgeblendet, weil eine
        /// Vorhersage-Kopie an ihrer Stelle angezeigt wird? Gilt nur lokal.
        /// </summary>
        public bool IsLocallyHidden { get; private set; }

        /// <summary>Der Collider der echten Kiste (für Physics.IgnoreCollision der Vorhersage).</summary>
        public Collider Collider => _collider;

        /// <summary>Gewicht in Kilogramm (aus dem Rigidbody), bestimmt, wie stark die Last bremst.</summary>
        public float Mass => _rigidbody != null ? _rigidbody.mass : 0f;

        /// <summary>Halbe Kantenlängen der Kiste in Metern (für die Wandprüfung beim Tragen).</summary>
        public Vector3 HalfExtents => _collider is BoxCollider box
            ? Vector3.Scale(box.size, transform.lossyScale) * 0.5f
            : transform.lossyScale * 0.5f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _networkRigidbody = GetComponent<NetworkRigidbody>();
            _collider = GetComponent<Collider>();
            _renderers = GetComponentsInChildren<Renderer>();
        }

        /// <summary>
        /// Blendet die echte Kiste nur auf diesem Rechner aus oder wieder ein.
        /// Ändert nichts am Netzwerkzustand, die anderen Spieler sehen sie weiter.
        /// </summary>
        public void SetLocallyHidden(bool hidden)
        {
            IsLocallyHidden = hidden;
            LocalGhost.SetVisible(_renderers, !hidden);
        }

        /// <summary>
        /// Erzeugt eine nicht vernetzte Darstellungs-Kopie mit gleicher Form, Größe und Farbe,
        /// an der aktuellen Weltposition. Nur für die Vorhersage auf dem eigenen Rechner.
        /// </summary>
        /// <param name="withPhysics">true: mit Collider und Rigidbody (zum Fallen und Fliegen).</param>
        public GameObject CreateLocalGhost(bool withPhysics)
        {
            GameObject ghost = LocalGhost.CreateFrom(transform);

            if (withPhysics)
            {
                // Ein BoxCollider passt sich beim Hinzufügen an das Mesh an.
                ghost.AddComponent<BoxCollider>();
                var body = ghost.AddComponent<Rigidbody>();
                body.mass = _rigidbody.mass;
            }

            return ghost;
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
