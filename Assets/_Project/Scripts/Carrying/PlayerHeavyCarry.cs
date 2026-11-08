using Schutzhuette.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Schutzhuette.Carrying
{
    /// <summary>
    /// Anpacken und Loslassen schwerer Gegenstände (<see cref="HeavyCarryable"/>) für die
    /// eigene Spielfigur, dazu die Laufgrenzen beim Tragen zu zweit.
    /// </summary>
    /// <remarks>
    /// Netcode: E schickt eine Bitte per RPC an den Host, der Host trägt die Figur als
    /// Halter ein oder aus (Architektur-Regel 2).
    /// Laufgrenzen: Die Bewegung der eigenen Figur bleibt beim Spieler (Owner-Autorität),
    /// deshalb prüft auch jeder Spieler selbst, ob er sich zu weit vom Partner entfernt
    /// oder ihm zu nahe kommt. Den Partner sieht er dabei um den Ping verzögert.
    /// </remarks>
    public class PlayerHeavyCarry : NetworkBehaviour, IMovementModifier
    {
        [Tooltip("Kopf-Punkt, von dem aus gezielt wird (derselbe wie bei PlayerLook).")]
        [SerializeField] private Transform _cameraMount;

        [Tooltip("Wie weit man einen schweren Gegenstand anvisieren kann, in Metern.")]
        [SerializeField] private float _interactRange = 2.5f;

        [Tooltip("Höchster Abstand Figur–Ende, den der Host beim Anpacken annimmt (Meter).")]
        [SerializeField] private float _maxServerGrabDistance = 2.5f;

        private InputAction _interactAction;
        private CharacterController _controller;
        private PlayerCarry _carry;
        private HeavyCarryable _current;
        private HeavyCarryable _lookTarget;
        private Collider _ignoredCollider;
        private bool _cursorWasLocked;
        private GUIStyle _hintStyle;

        // Vorhersage beim Tragen zu zweit (nur Clients): Kopie der Truhe aus eigener Sicht.
        private GameObject _chestGhost;
        private HeavyCarryable _hiddenChest;

        /// <summary>
        /// Hält diese Figur gerade ein Ende eines schweren Gegenstands?
        /// Aussagekräftig auf dem eigenen Rechner und auf dem Host.
        /// </summary>
        public bool IsHolding => _current != null && _current.IsHeldBy(NetworkObject, out _);

        /// <inheritdoc />
        public float SpeedMultiplier => IsHolding && _current.HolderCount == 2 ? _current.CarrySpeedMultiplier : 1f;

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                return;
            }

            _controller = GetComponent<CharacterController>();
            _carry = GetComponent<PlayerCarry>();

            if (InputSystem.actions != null)
            {
                _interactAction = InputSystem.actions.FindAction("Player/Interact");
                _interactAction?.Enable();
            }
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            SetIgnoreCollision(null);
            EndChestPrediction();
        }

        // Nach dem Laufen (Update) die Vorhersage-Kopie an die neue eigene Position setzen.
        // Der Host braucht das nicht: Er rechnet die echte Truhe schon aus seiner Sicht aus.
        private void LateUpdate()
        {
            if (!IsSpawned || !IsOwner || IsServer)
            {
                return;
            }

            if (!IsHolding || !_current.TryGetPartner(NetworkObject, out NetworkObject partner))
            {
                EndChestPrediction();
                return;
            }

            // Inzwischen eine andere Truhe angepackt: alte Vorhersage sauber beenden.
            if (_hiddenChest != null && _hiddenChest != _current)
            {
                EndChestPrediction();
            }

            if (_chestGhost == null)
            {
                _chestGhost = _current.CreateLocalGhost();
                _hiddenChest = _current;
                _hiddenChest.SetLocallyHidden(true);
            }

            // Dieselbe Rechnung wie beim Host, aber mit der eigenen, aktuellen Position
            // statt der verzögerten. Den Partner nehmen wir so, wie wir ihn gerade sehen.
            _current.IsHeldBy(NetworkObject, out int myEnd);
            Vector3 me = transform.position;
            Vector3 other = partner.transform.position;
            _current.ComputePose(myEnd == 0 ? me : other, myEnd == 0 ? other : me,
                out Vector3 center, out Quaternion rotation);
            _chestGhost.transform.SetPositionAndRotation(center, rotation);
        }

        private void EndChestPrediction()
        {
            if (_chestGhost != null)
            {
                Destroy(_chestGhost);
            }

            if (_hiddenChest != null)
            {
                _hiddenChest.SetLocallyHidden(false);
            }

            _chestGhost = null;
            _hiddenChest = null;
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner)
            {
                return;
            }

            bool holding = IsHolding;

            // Beim Tragen soll der Gegenstand die eigene Figur nicht wegschieben oder blockieren.
            SetIgnoreCollision(holding ? _current.Collider : null);

            bool locked = Cursor.lockState == CursorLockMode.Locked;
            bool inputAllowed = locked && _cursorWasLocked;
            _cursorWasLocked = locked;

            bool carryBusy = _carry != null && _carry.IsBusy;
            _lookTarget = !holding && !carryBusy ? FindLookTarget() : null;

            if (!inputAllowed || _interactAction == null || !_interactAction.WasPressedThisFrame())
            {
                return;
            }

            if (holding)
            {
                ReleaseHeavyRpc(_current.NetworkObject);
            }
            else if (_lookTarget != null)
            {
                _current = _lookTarget;
                GrabHeavyRpc(_lookTarget.NetworkObject, _lookTarget.NearestEnd(transform.position));
            }
        }

        /// <inheritdoc />
        public Vector3 ConstrainMove(Vector3 currentPosition, Vector3 desiredDelta)
        {
            if (!IsHolding)
            {
                return desiredDelta;
            }

            // Zu zweit: Abstand zum Partner zwischen Minimum und Maximum halten,
            // und nicht so laufen, dass die Truhe dabei in eine Wand ragt.
            if (_current.TryGetPartner(NetworkObject, out NetworkObject partner))
            {
                Vector3 partnerPosition = partner.transform.position;
                Vector3 delta = KeepDistance(currentPosition, desiredDelta, partnerPosition,
                    _current.MinCarrierDistance, _current.MaxCarrierDistance);

                _current.IsHeldBy(NetworkObject, out int myEnd);
                Vector3 halfExtents = _current.HalfExtents;

                // Steckt die Truhe schon fest (z. B. gerade so angehoben), nicht festhalten.
                if (ChestBlockedAt(currentPosition, partnerPosition, myEnd, halfExtents))
                {
                    return delta;
                }

                return CarryCollision.Slide(delta,
                    step => ChestBlockedAt(currentPosition + step, partnerPosition, myEnd, halfExtents));
            }

            // Allein: Der Gegenstand bewegt sich nicht, man kommt nicht von seinem Ende weg.
            _current.IsHeldBy(NetworkObject, out int end);
            return KeepDistance(currentPosition, desiredDelta, _current.GetEndPosition(end),
                0f, _current.SingleHolderRadius);
        }

        /// <inheritdoc />
        /// <remarks>Die Lage der Truhe hängt nur von den Positionen der Träger ab, Drehen ändert nichts.</remarks>
        public float ConstrainYaw(Vector3 currentPosition, Quaternion currentRotation, float desiredYawDegrees)
        {
            return desiredYawDegrees;
        }

        // Ragt die Truhe in feste Umgebung, wenn ich hier stehe und der Partner dort?
        private bool ChestBlockedAt(Vector3 myPosition, Vector3 partnerPosition, int myEnd, Vector3 halfExtents)
        {
            Vector3 holderOfEnd0 = myEnd == 0 ? myPosition : partnerPosition;
            Vector3 holderOfEnd1 = myEnd == 0 ? partnerPosition : myPosition;
            _current.ComputePose(holderOfEnd0, holderOfEnd1, out Vector3 center, out Quaternion rotation);
            return CarryCollision.HitsEnvironment(center, halfExtents, rotation);
        }

        /// <summary>
        /// Begrenzt eine waagrechte Verschiebung so, dass der Abstand zu einem Punkt nicht
        /// über max wächst und nicht unter min fällt. Seitliche Bewegung bleibt erlaubt,
        /// nur der Anteil in Richtung zum Punkt hin oder von ihm weg wird gestrichen.
        /// </summary>
        public static Vector3 KeepDistance(Vector3 position, Vector3 delta, Vector3 other, float min, float max)
        {
            Vector3 offsetNow = position - other;
            offsetNow.y = 0f;
            Vector3 offsetNext = position + delta - other;
            offsetNext.y = 0f;

            float distanceNow = offsetNow.magnitude;
            float distanceNext = offsetNext.magnitude;
            if (distanceNow < 0.0001f)
            {
                return delta;
            }

            Vector3 outward = offsetNow / distanceNow;
            float radial = Vector3.Dot(delta, outward);

            bool tooFar = distanceNext > max && radial > 0f;
            bool tooClose = distanceNext < min && radial < 0f;
            return tooFar || tooClose ? delta - outward * radial : delta;
        }

        private HeavyCarryable FindLookTarget()
        {
            if (_cameraMount == null
                || !Physics.Raycast(_cameraMount.position, _cameraMount.forward, out RaycastHit hit,
                    _interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return null;
            }

            var heavy = hit.collider.GetComponentInParent<HeavyCarryable>();
            return heavy != null && heavy.HolderCount < 2 ? heavy : null;
        }

        private void SetIgnoreCollision(Collider target)
        {
            if (_controller == null || _ignoredCollider == target)
            {
                return;
            }

            if (_ignoredCollider != null)
            {
                Physics.IgnoreCollision(_controller, _ignoredCollider, false);
            }

            if (target != null)
            {
                Physics.IgnoreCollision(_controller, target, true);
            }

            _ignoredCollider = target;
        }

        // ----- RPCs, laufen auf dem Host -----

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void GrabHeavyRpc(NetworkObjectReference heavyReference, int end)
        {
            if (!heavyReference.TryGet(out NetworkObject heavyObject)
                || !heavyObject.TryGetComponent(out HeavyCarryable heavy))
            {
                return;
            }

            // Hält die Figur schon eine Kiste, darf sie nicht zusätzlich anpacken.
            var carry = GetComponent<PlayerCarry>();
            if (carry != null && carry.HeldItem != null)
            {
                return;
            }

            if (Vector3.Distance(transform.position, heavy.GetEndPosition(end)) > _maxServerGrabDistance)
            {
                return;
            }

            // Auch der Host merkt sich, was diese Figur hält (für IsHolding auf dem Host).
            if (heavy.TryGrab(NetworkObject, end))
            {
                _current = heavy;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void ReleaseHeavyRpc(NetworkObjectReference heavyReference)
        {
            if (heavyReference.TryGet(out NetworkObject heavyObject)
                && heavyObject.TryGetComponent(out HeavyCarryable heavy))
            {
                heavy.Release(NetworkObject);
            }
        }

        private void OnGUI()
        {
            if (!IsSpawned || !IsOwner)
            {
                return;
            }

            string hint = null;
            if (IsHolding)
            {
                hint = _current.HolderCount == 2
                    ? "Zu zweit tragen     E: loslassen"
                    : "Warte auf zweiten Träger     E: loslassen";
            }
            else if (_lookTarget != null)
            {
                hint = "E: Ende anpacken (nur zu zweit tragbar)";
            }

            if (hint == null)
            {
                return;
            }

            GUI.matrix = Matrix4x4.identity;
            _hintStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
            };

            GUI.Label(new Rect(Screen.width * 0.5f - 250f, Screen.height - 100f, 500f, 30f), hint, _hintStyle);
        }
    }
}
