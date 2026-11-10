using Unity.Netcode;
using UnityEngine;

namespace Schutzhuette.Items
{
    /// <summary>
    /// Der Aufprall-Messer an jedem Gegenstand. Arbeitet nur beim Host und ist die einzige Stelle, die
    /// OnCollisionEnter für Spiel-Logik auswertet (ADR-0001, Entscheidungen 2 bis 5).
    /// </summary>
    /// <remarks>
    /// Er hält auch den Zustand Liegt, Getragen oder Fällt und gleicht daraus die Kollisionsprüfung ab:
    /// durchgehend ("ContinuousDynamic") nur, solange der Gegenstand fällt und nicht kinematisch ist,
    /// sonst "Discrete". So fliegt ein schneller Wurf nicht durch eine dünne Wand, und ein kinematischer
    /// Körper bekommt nie die durchgehende Prüfung, die Unity bei ihm nicht unterstützt.
    /// Andere Systeme werten keine Kollisionen aus, sie hören am Meldedienst <see cref="ImpactEvents"/> zu.
    /// </remarks>
    /// <example>
    /// Das Tragen meldet, wenn es einen Gegenstand aufnimmt oder loslässt:
    /// <code>
    /// reporter.MarkCarried();   // bevor der Körper kinematisch wird
    /// reporter.MarkFalling();   // nachdem er losgelassen wurde
    /// </code>
    /// </example>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class ImpactReporter : NetworkBehaviour
    {
        private Rigidbody _body;

        // Nur beim Host und nur, solange der Gegenstand gespawnt ist.
        private ImpactJudge _judge;
        private bool _isFalling;

        /// <summary>Zustand des Gegenstands beim Host. Bei Clients ohne Bedeutung.</summary>
        public ItemPhysicsState State
        {
            get
            {
                if (_body != null && _body.isKinematic)
                {
                    return ItemPhysicsState.Carried;
                }

                return _isFalling ? ItemPhysicsState.Falling : ItemPhysicsState.Resting;
            }
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
        }

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                return;
            }

            ItemKind kind = TryGetComponent(out Item item) ? item.Kind : null;
            bool isFragile = kind != null && kind.Fragile.Enabled;
            float breakSpeed = kind != null ? kind.Fragile.BreakSpeed : 0f;

            // Die beiden Funktionen entstehen einmal hier, nicht bei jedem Aufprall.
            _judge = new ImpactJudge(isFragile, breakSpeed, _body.mass, ReportImpact, RemoveItem);
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            _judge = null;
        }

        /// <summary>
        /// Nur beim Host: Der Gegenstand wird gleich getragen. Muss aufgerufen werden, bevor eigener Code
        /// den Körper kinematisch macht, damit er vorher wieder "Discrete" prüft.
        /// </summary>
        public void MarkCarried()
        {
            if (_judge == null)
            {
                return;
            }

            _isFalling = false;
            SetDetection(CollisionDetectionMode.Discrete);
        }

        /// <summary>
        /// Nur beim Host: Der Gegenstand wurde losgelassen, geworfen oder angestoßen. Die durchgehende
        /// Kollisionsprüfung gilt sofort, nicht erst im nächsten Physikschritt.
        /// </summary>
        public void MarkFalling()
        {
            if (_judge == null || _body.isKinematic)
            {
                return;
            }

            _isFalling = true;
            SetDetection(CollisionDetectionMode.ContinuousDynamic);
        }

        // Die eine Stelle, die Zustand und Kollisionsprüfung jeden Physikschritt abgleicht.
        private void FixedUpdate()
        {
            if (_judge == null)
            {
                return;
            }

            if (_body.isKinematic)
            {
                // Getragen, auch wenn NGO oder anderer Code den Körper kinematisch gemacht hat.
                _isFalling = false;
            }
            else if (_isFalling && _body.IsSleeping())
            {
                // Zur Ruhe gekommen: liegt wieder.
                _isFalling = false;
            }

            SetDetection(_isFalling ? CollisionDetectionMode.ContinuousDynamic : CollisionDetectionMode.Discrete);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_judge == null || !IsSpawned)
            {
                return;
            }

            // Größter senkrechter Anteil über alle Kontaktpunkte, ohne Allokation gelesen.
            float impactSpeed = 0f;
            Vector3 point = transform.position;
            int contactCount = collision.contactCount;
            for (int i = 0; i < contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                float speed = BreakRule.ImpactSpeed(collision.relativeVelocity, contact.normal);
                if (speed >= impactSpeed)
                {
                    impactSpeed = speed;
                    point = contact.point;
                }
            }

            // Ein liegender Gegenstand, den ein bewegter Körper trifft, fällt ab diesem Moment. Das muss
            // vor der Bruchregel passieren: Welcher der beiden Körper seinen Callback zuerst bekommt,
            // ist nicht festgelegt, und ohne diese Reihenfolge zerbräche eine liegende Tasse nie.
            Rigidbody otherBody = collision.rigidbody;
            if (!_isFalling && otherBody != null && !otherBody.isKinematic)
            {
                MarkFalling();
            }

            _judge.HandleImpact(NetworkObject, point, impactSpeed, _isFalling, collision.collider);
        }

        private void SetDetection(CollisionDetectionMode mode)
        {
            if (_body.collisionDetectionMode != mode)
            {
                _body.collisionDetectionMode = mode;
            }
        }

        private void ReportImpact(ImpactReport report)
        {
            ImpactEvents.Raise(report);
        }

        private void RemoveItem()
        {
            ItemSpawning.Remove(NetworkObject);
        }
    }
}
