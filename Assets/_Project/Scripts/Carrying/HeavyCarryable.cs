using Schutzhuette.Items;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Schutzhuette.Carrying
{
    /// <summary>
    /// Ein schwerer, langer Gegenstand mit zwei Enden, der sich nur bewegt, wenn an jedem
    /// Ende ein Spieler anpackt. Gehört immer dem Host (Server-Autorität).
    /// </summary>
    /// <remarks>
    /// Wer welches Ende hält, steht in zwei NetworkVariables, die nur der Host schreibt.
    /// Gespeichert wird die Netzwerk-Referenz der Spielfigur, weil Clients in NGO die
    /// Figuren anderer Spieler nicht über deren Client-ID nachschlagen können.
    /// Halten beide Enden, rechnet der Host jedes Frame die Lage aus den Positionen der
    /// beiden Träger aus (Regel 3: getragene Dinge folgen dem Träger, nicht der Physik)
    /// und der NetworkTransform verteilt sie. Sonst liegt der Gegenstand und der Host
    /// rechnet seine Physik.
    /// Die lange Seite ist die lokale z-Achse: Ende 0 hinten (-z), Ende 1 vorne (+z).
    /// </remarks>
    [RequireComponent(typeof(Rigidbody), typeof(NetworkRigidbody))]
    public class HeavyCarryable : NetworkBehaviour
    {
        [Tooltip("Abstand der beiden Enden voneinander, in Metern (lange Seite).")]
        [SerializeField] private float _length = 1.8f;

        [Tooltip("Abstand von der Körpermitte eines Trägers bis zu seiner Hand, in Metern.")]
        [SerializeField] private float _reach = 0.7f;

        [Tooltip("Höhe der Hände über der Körpermitte (Kapselmitte), in Metern.")]
        [SerializeField] private float _handHeight = 0.2f;

        [Tooltip("Näher als dies dürfen die zwei Träger einander nicht kommen (waagrecht, Meter).")]
        [SerializeField] private float _minCarrierDistance = 2.6f;

        [Tooltip("Weiter als dies dürfen die zwei Träger nicht auseinander (waagrecht, Meter).")]
        [SerializeField] private float _maxCarrierDistance = 3.6f;

        [Tooltip("Hält nur einer, kommt er so weit von seinem Ende weg (waagrecht, Meter).")]
        [SerializeField] private float _singleHolderRadius = 1.2f;

        [Tooltip("Laufgeschwindigkeit beim Tragen zu zweit, als Faktor (1 = normal).")]
        [SerializeField, Range(0.1f, 1f)] private float _carrySpeedMultiplier = 0.6f;

        private readonly NetworkVariable<NetworkObjectReference> _holder0 = new NetworkVariable<NetworkObjectReference>();
        private readonly NetworkVariable<NetworkObjectReference> _holder1 = new NetworkVariable<NetworkObjectReference>();

        private Rigidbody _rigidbody;
        private NetworkRigidbody _networkRigidbody;
        private Collider _collider;
        private Renderer[] _renderers;
        private bool _carriedOnServer;
        private Vector3 _lastCarryCenter;
        private float _swaySpeed;

        /// <summary>
        /// Leichtes Schaukeln einer getragenen Truhe: bis 3 Grad seitlich und 1,8 Grad
        /// nach vorn und hinten, je schneller getragen wird (ab 2 m/s volle Stärke).
        /// Reine Rechnung ohne Zustand, damit Host und Vorhersage gleich schaukeln.
        /// </summary>
        public static Quaternion Sway(float time, float speed)
        {
            float amount = Mathf.Clamp01(speed / 2f) * 3f;
            return Quaternion.Euler(Mathf.Sin(time * 2.7f) * amount * 0.6f, 0f, Mathf.Sin(time * 4f) * amount);
        }

        /// <summary>Waagrechter Mindestabstand der zwei Träger.</summary>
        public float MinCarrierDistance => _minCarrierDistance;

        /// <summary>Waagrechter Höchstabstand der zwei Träger.</summary>
        public float MaxCarrierDistance => _maxCarrierDistance;

        /// <summary>Wie weit sich ein einzelner Halter von seinem Ende entfernen kann.</summary>
        public float SingleHolderRadius => _singleHolderRadius;

        /// <summary>Faktor für die Laufgeschwindigkeit beim Tragen zu zweit.</summary>
        public float CarrySpeedMultiplier => _carrySpeedMultiplier;

        /// <summary>Abstand Körpermitte → Hand eines Trägers (für die greifenden Arme der Figur).</summary>
        public float Reach => _reach;

        /// <summary>Höhe der Hände über der Körpermitte.</summary>
        public float HandHeight => _handHeight;

        /// <summary>Der Collider des Gegenstands (die Träger ignorieren ihn beim Tragen).</summary>
        public Collider Collider => _collider;

        /// <summary>Halbe Kantenlängen in Metern (für die Wandprüfung beim Tragen).</summary>
        public Vector3 HalfExtents => _collider is BoxCollider box
            ? Vector3.Scale(box.size, transform.lossyScale) * 0.5f
            : transform.lossyScale * 0.5f;

        /// <summary>
        /// Lage, die der Gegenstand hätte, wenn die Träger der beiden Enden an diesen Stellen
        /// stünden. Nutzt dieselben Einstellungen wie das Tragen auf dem Host.
        /// </summary>
        /// <param name="holderOfEnd0">Position der Figur an Ende 0.</param>
        /// <param name="holderOfEnd1">Position der Figur an Ende 1.</param>
        public void ComputePose(Vector3 holderOfEnd0, Vector3 holderOfEnd1, out Vector3 center, out Quaternion rotation)
        {
            ComputeCarryPose(holderOfEnd0, holderOfEnd1, _reach, _handHeight, out center, out rotation);
        }

        /// <summary>Wie viele Enden gerade gehalten werden (0 bis 2). Stimmt auf Host und Clients.</summary>
        public int HolderCount => (TryGetHolder(0, out _) ? 1 : 0) + (TryGetHolder(1, out _) ? 1 : 0);

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _networkRigidbody = GetComponent<NetworkRigidbody>();
            _collider = GetComponent<Collider>();
            _renderers = GetComponentsInChildren<Renderer>();

            // Jeder tragbare Gegenstand braucht ein Etikett mit seiner Art.
            Item.ReportIfMissing(this);
        }

        /// <summary>
        /// Blendet den echten Gegenstand nur auf diesem Rechner aus oder ein, solange eine
        /// Vorhersage-Kopie an seiner Stelle steht. Ändert nichts am Netzwerkzustand.
        /// </summary>
        public void SetLocallyHidden(bool hidden)
        {
            LocalGhost.SetVisible(_renderers, !hidden);
        }

        /// <summary>Nicht vernetzte Darstellungs-Kopie für die Vorhersage (ohne Collider).</summary>
        public GameObject CreateLocalGhost()
        {
            return LocalGhost.CreateFrom(transform);
        }

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            // Figuren erfahren auf jedem Rechner, ob sie ein Ende halten (für die Arme).
            _holder0.OnValueChanged += HandleHolderChanged;
            _holder1.OnValueChanged += HandleHolderChanged;
            HandleHolderChanged(default, _holder0.Value);
            HandleHolderChanged(default, _holder1.Value);
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            _holder0.OnValueChanged -= HandleHolderChanged;
            _holder1.OnValueChanged -= HandleHolderChanged;
            HandleHolderChanged(_holder0.Value, default);
            HandleHolderChanged(_holder1.Value, default);
        }

        private void HandleHolderChanged(NetworkObjectReference previous, NetworkObjectReference current)
        {
            if (previous.TryGet(out NetworkObject oldHolder)
                && oldHolder.TryGetComponent(out Schutzhuette.Player.PlayerVisuals oldVisuals))
            {
                oldVisuals.ClearHeldChest(this);
            }

            if (current.TryGet(out NetworkObject newHolder)
                && newHolder.TryGetComponent(out Schutzhuette.Player.PlayerVisuals newVisuals))
            {
                newVisuals.SetHeldChest(this);
            }
        }

        /// <summary>Weltposition eines Endes (0 = hinten, 1 = vorne).</summary>
        public Vector3 GetEndPosition(int end)
        {
            float sign = end == 0 ? -1f : 1f;
            return transform.position + transform.forward * (sign * _length * 0.5f);
        }

        /// <summary>Das Ende, das einem Punkt am nächsten liegt.</summary>
        public int NearestEnd(Vector3 point)
        {
            return (point - GetEndPosition(0)).sqrMagnitude <= (point - GetEndPosition(1)).sqrMagnitude ? 0 : 1;
        }

        /// <summary>Die Spielfigur, die ein Ende hält. Stimmt auf Host und Clients.</summary>
        public bool TryGetHolder(int end, out NetworkObject holder)
        {
            NetworkObjectReference reference = end == 0 ? _holder0.Value : _holder1.Value;
            return reference.TryGet(out holder);
        }

        /// <summary>Hält diese Spielfigur ein Ende, und wenn ja, welches?</summary>
        public bool IsHeldBy(NetworkObject player, out int end)
        {
            for (end = 0; end < 2; end++)
            {
                if (TryGetHolder(end, out NetworkObject holder) && holder == player)
                {
                    return true;
                }
            }

            end = -1;
            return false;
        }

        /// <summary>Die Spielfigur am anderen Ende, falls dort jemand anpackt.</summary>
        public bool TryGetPartner(NetworkObject player, out NetworkObject partner)
        {
            partner = null;
            return IsHeldBy(player, out int end) && TryGetHolder(1 - end, out partner);
        }

        /// <summary>Nur auf dem Host: Spielfigur packt ein Ende an.</summary>
        /// <returns>true, wenn das Ende frei war und die Figur noch nichts hielt.</returns>
        public bool TryGrab(NetworkObject player, int end)
        {
            if (!IsServer || end < 0 || end > 1 || IsHeldBy(player, out _) || TryGetHolder(end, out _))
            {
                return false;
            }

            if (end == 0)
            {
                _holder0.Value = player;
            }
            else
            {
                _holder1.Value = player;
            }

            UpdateCarryState();
            return true;
        }

        /// <summary>Nur auf dem Host: Spielfigur lässt ihr Ende los.</summary>
        public void Release(NetworkObject player)
        {
            if (!IsServer)
            {
                return;
            }

            if (TryGetHolder(0, out NetworkObject holder0) && holder0 == player)
            {
                _holder0.Value = default;
            }

            if (TryGetHolder(1, out NetworkObject holder1) && holder1 == player)
            {
                _holder1.Value = default;
            }

            UpdateCarryState();
        }

        /// <summary>
        /// Lage des Gegenstands zwischen zwei Trägern: Jede Hand sitzt ein Stück vor der
        /// Körpermitte in Richtung des anderen Trägers, der Gegenstand liegt mittig zwischen
        /// den Händen, seine lange Seite zeigt von Träger A zu Träger B. Reine Rechnung ohne
        /// Zustand, damit sie später auch die Vorhersage auf den Clients nutzen kann.
        /// </summary>
        public static void ComputeCarryPose(Vector3 carrierA, Vector3 carrierB, float reach, float handHeight,
            out Vector3 center, out Quaternion rotation)
        {
            Vector3 flat = carrierB - carrierA;
            flat.y = 0f;
            Vector3 direction = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;

            Vector3 handA = carrierA + direction * reach + Vector3.up * handHeight;
            Vector3 handB = carrierB - direction * reach + Vector3.up * handHeight;

            center = (handA + handB) * 0.5f;
            Vector3 along = handB - handA;
            rotation = along.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(along, Vector3.up) : Quaternion.LookRotation(direction);
        }

        private void LateUpdate()
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            RemoveVanishedHolders();

            if (_carriedOnServer
                && TryGetHolder(0, out NetworkObject carrierA)
                && TryGetHolder(1, out NetworkObject carrierB))
            {
                ComputeCarryPose(carrierA.transform.position, carrierB.transform.position, _reach, _handHeight,
                    out Vector3 center, out Quaternion rotation);

                // Leicht schaukeln, je schneller getragen wird, desto mehr.
                float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
                float centerSpeed = (center - _lastCarryCenter).magnitude / deltaTime;
                _lastCarryCenter = center;
                _swaySpeed = Mathf.Lerp(_swaySpeed, Mathf.Min(centerSpeed, 5f), 5f * deltaTime);

                transform.SetPositionAndRotation(center, rotation * Sway(Time.time, _swaySpeed));
            }
        }

        // Ein Träger hat sich getrennt: sein Ende ist wieder frei.
        private void RemoveVanishedHolders()
        {
            bool changed = false;
            if (!_holder0.Value.Equals(default(NetworkObjectReference)) && !TryGetHolder(0, out _))
            {
                _holder0.Value = default;
                changed = true;
            }

            if (!_holder1.Value.Equals(default(NetworkObjectReference)) && !TryGetHolder(1, out _))
            {
                _holder1.Value = default;
                changed = true;
            }

            if (changed)
            {
                UpdateCarryState();
            }
        }

        // Zu zweit: kinematisch, folgt den Trägern. Sonst: wieder Physik beim Host (fällt herunter).
        private void UpdateCarryState()
        {
            bool carried = HolderCount == 2;
            if (carried == _carriedOnServer)
            {
                return;
            }

            _carriedOnServer = carried;
            if (carried)
            {
                // Schaukeln ruhig beginnen lassen.
                _lastCarryCenter = transform.position;
                _swaySpeed = 0f;

                if (!_rigidbody.isKinematic)
                {
                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                }

                // Vor dem Kinematisch-Machen: Der Aufprall-Messer stellt die Kollisionsprüfung zurück auf
                // "Discrete", falls die Truhe gerade angestoßen wurde und noch als fallend gilt.
                if (TryGetComponent(out ImpactReporter impactReporter))
                {
                    impactReporter.MarkCarried();
                }

                _networkRigidbody.SetIsKinematic(true);
            }
            else
            {
                _networkRigidbody.SetIsKinematic(false);
                _rigidbody.WakeUp();
            }
        }
    }
}
