using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Schutzhuette.Carrying
{
    /// <summary>
    /// Aufheben, Ablegen und Werfen für die eigene Spielfigur, dazu Fadenkreuz und Tastenhinweis.
    /// E hebt auf oder legt ab, Linksklick wirft.
    /// </summary>
    /// <remarks>
    /// Netcode: Der Spieler entscheidet nicht selbst. Er schickt eine Bitte per RPC an den
    /// Host ([Rpc(SendTo.Server)]), der Host prüft sie und führt sie aus (Architektur-Regel 2).
    /// Beim Ablegen und Werfen schickt der Spieler seine eigene Sicht der Kistenposition mit:
    /// Bei ihm klebt die Kiste ohne Verzögerung vor der Figur, der Host sieht die Figur
    /// dagegen um den Ping verzögert. Der Host übernimmt die Position nur, wenn sie
    /// plausibel nah an der Figur liegt.
    ///
    /// Client-Vorhersage: Bis die Antwort des Hosts da ist, vergeht etwa ein Ping plus
    /// Glättung. Damit sich das nicht träge anfühlt, zeigt der eigene Rechner sofort eine
    /// nicht vernetzte Kopie der Kiste (in den Händen beim Aufheben, fliegend mit eigener
    /// Physik beim Loslassen) und blendet die echte Kiste so lange aus. Sobald der echte
    /// Zustand vom Host angekommen ist, wird zurückgetauscht. Entscheidet der Host anders
    /// (jemand war schneller), verschwindet die Kopie einfach wieder. Die anderen Spieler
    /// sehen von alledem nichts, nur die echte Kiste.
    /// </remarks>
    public class PlayerCarry : NetworkBehaviour
    {
        [Tooltip("Kopf-Punkt, von dem aus gezielt wird (derselbe wie bei PlayerLook).")]
        [SerializeField] private Transform _cameraMount;

        [Tooltip("Wie weit man eine Kiste anvisieren kann, in Metern.")]
        [SerializeField] private float _interactRange = 2.5f;

        [Tooltip("Haltepunkt der Kiste relativ zur Spielfigur (Mitte der Kapsel).")]
        [SerializeField] private Vector3 _holdLocalPosition = new Vector3(0f, 0.2f, 0.9f);

        [Tooltip("Wurfgeschwindigkeit in Blickrichtung, in m/s.")]
        [SerializeField] private float _throwSpeed = 8f;

        [Tooltip("Höchste Wurfgeschwindigkeit, die der Host annimmt.")]
        [SerializeField] private float _maxThrowSpeed = 15f;

        [Tooltip("Höchster Abstand Figur–Kiste, den der Host bei Bitten annimmt.")]
        [SerializeField] private float _maxServerDistance = 4f;

        [Header("Vorhersage")]
        [Tooltip("So lange wartet die Aufheb-Vorhersage höchstens auf den Host, in Sekunden.")]
        [SerializeField] private float _grabTimeout = 0.75f;

        [Tooltip("So lange darf eine geworfene Vorhersage-Kopie höchstens fliegen, in Sekunden.")]
        [SerializeField] private float _releaseMaxTime = 2.5f;

        [Tooltip("Liegen Kopie und echte Kiste näher als dies (Meter), wird getauscht.")]
        [SerializeField] private float _swapDistance = 0.3f;

        private InputAction _interactAction;
        private InputAction _throwAction;
        private CharacterController _controller;
        private Carryable _lookTarget;
        private bool _cursorWasLocked;
        private GUIStyle _hintStyle;

        // Aufheb-Vorhersage: Kopie in den Händen, bis der Host die echte Kiste anhängt.
        private Carryable _pendingGrab;
        private GameObject _grabGhost;
        private float _grabStartTime;

        // Loslass-Vorhersage: fliegende Kopie, bis die echte Kiste (vom Host) aufgeholt hat.
        private Carryable _releasedItem;
        private GameObject _releaseGhost;
        private Rigidbody _releaseGhostBody;
        private Collider _releaseGhostCollider;
        private float _releaseStartTime;

        /// <summary>Der Gegenstand, den diese Figur gerade trägt, sonst null. Stimmt auf Host und Clients.</summary>
        public Carryable HeldItem => GetComponentInChildren<Carryable>();

        /// <summary>
        /// Trägt der Spieler aus eigener Sicht gerade eine Kiste oder hebt eine auf?
        /// Nur auf dem eigenen Rechner aussagekräftig.
        /// </summary>
        public bool IsBusy => CurrentlyHeld() != null || _pendingGrab != null;

        private PlayerHeavyCarry _heavyCarry;

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                return;
            }

            _controller = GetComponent<CharacterController>();
            _heavyCarry = GetComponent<PlayerHeavyCarry>();

            if (InputSystem.actions != null)
            {
                _interactAction = InputSystem.actions.FindAction("Player/Interact");
                _throwAction = InputSystem.actions.FindAction("Player/Attack");
                _interactAction?.Enable();
                _throwAction?.Enable();
            }

            if (_interactAction == null || _throwAction == null)
            {
                Debug.LogWarning("[Carry] Input Actions \"Player/Interact\" oder \"Player/Attack\" nicht gefunden.", this);
            }
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                EndGrabPrediction();
                EndReleasePrediction();
            }
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner)
            {
                return;
            }

            UpdateGrabPrediction();
            UpdateReleasePrediction();

            // Nicht in dem Frame reagieren, in dem ein Klick die Maus erst einfängt,
            // sonst wirft der Klick zum Einfangen gleich die Kiste.
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            bool inputAllowed = locked && _cursorWasLocked;
            _cursorWasLocked = locked;

            Carryable held = CurrentlyHeld();
            bool grabPending = _pendingGrab != null;

            // Wer gerade einen schweren Gegenstand hält, kann nicht zusätzlich eine Kiste nehmen.
            if (_heavyCarry != null && _heavyCarry.IsHolding)
            {
                _lookTarget = null;
                return;
            }

            _lookTarget = held == null && !grabPending ? FindLookTarget() : null;

            if (!inputAllowed)
            {
                return;
            }

            bool interactPressed = _interactAction != null && _interactAction.WasPressedThisFrame();
            bool throwPressed = _throwAction != null && _throwAction.WasPressedThisFrame();

            // Schon losgelassen, bevor der Host das Aufheben bestätigt hat: nicht warten,
            // sondern sofort mit der Kopie aus den Händen weitermachen.
            if (grabPending)
            {
                if (interactPressed)
                {
                    ReleasePendingGrab(Vector3.zero);
                }
                else if (throwPressed)
                {
                    ReleasePendingGrab(ThrowVelocity());
                }

                return;
            }

            if (interactPressed)
            {
                if (held != null)
                {
                    Release(held, Vector3.zero);
                }
                else if (_lookTarget != null)
                {
                    PickUp(_lookTarget);
                }
            }
            else if (held != null && throwPressed)
            {
                Release(held, ThrowVelocity());
            }
        }

        // Blickrichtung mal Wurftempo, plus eigene Laufgeschwindigkeit, sonst wirkt ein
        // Wurf aus dem Laufen zu schwach.
        private Vector3 ThrowVelocity()
        {
            Vector3 velocity = _cameraMount.forward * _throwSpeed;
            if (_controller != null)
            {
                velocity += _controller.velocity;
            }

            return velocity;
        }

        private void PickUp(Carryable item)
        {
            // Die gerade geworfene Kiste wieder aufheben: Flug-Kopie beenden, Aufheb-Kopie übernimmt.
            if (item == _releasedItem)
            {
                EndReleasePrediction();
            }

            PickUpRpc(item.NetworkObject);

            // Der Host führt seine eigene Bitte sofort aus, er braucht keine Vorhersage.
            if (!IsServer)
            {
                StartGrabPrediction(item);
            }
        }

        // Nur auf Clients: Loslassen, während die Aufheb-Kopie noch auf die Bestätigung wartet.
        // Die Bitten kommen beim Host in derselben Reihenfolge an (erst aufheben, dann loslassen),
        // er führt also beide nacheinander aus.
        private void ReleasePendingGrab(Vector3 velocity)
        {
            Carryable item = _pendingGrab;
            Vector3 position = _grabGhost.transform.position;
            Quaternion rotation = _grabGhost.transform.rotation;

            // Aufheb-Kopie weg, die echte Kiste bleibt ausgeblendet: die Flug-Kopie übernimmt.
            Destroy(_grabGhost);
            _grabGhost = null;
            _pendingGrab = null;

            DropRpc(position, rotation, velocity);
            StartReleasePrediction(item, velocity, position, rotation);
        }

        // Was der Spieler aus seiner Sicht gerade trägt. Eine schon losgelassene Kiste hängt
        // bis zur Antwort des Hosts noch an der Figur, zählt aber nicht mehr.
        private Carryable CurrentlyHeld()
        {
            Carryable held = HeldItem;
            return held != null && held == _releasedItem ? null : held;
        }

        private void Release(Carryable held, Vector3 velocity)
        {
            // Pose und Geschwindigkeit vor dem RPC merken: Auf dem Host läuft DropRpc sofort.
            Vector3 position = held.transform.position;
            Quaternion rotation = held.transform.rotation;
            DropRpc(position, rotation, velocity);

            if (!IsServer)
            {
                StartReleasePrediction(held, velocity, position, rotation);
            }
        }

        // Kiste in Blickrichtung, die gerade frei und sichtbar ist, sonst null.
        private Carryable FindLookTarget()
        {
            if (_cameraMount == null)
            {
                return null;
            }

            // Der Strahl startet im eigenen CharacterController. Collider, in denen ein
            // Strahl beginnt, trifft er nicht, deshalb stört die eigene Figur nicht.
            if (!Physics.Raycast(_cameraMount.position, _cameraMount.forward, out RaycastHit hit,
                    _interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return null;
            }

            // Die Flug-Kopie einer gerade geworfenen Kiste steht für die echte Kiste.
            // Auch die unsichtbare echte Kiste selbst kann getroffen werden.
            Carryable item = _releaseGhostCollider != null && hit.collider == _releaseGhostCollider
                ? _releasedItem
                : hit.collider.GetComponentInParent<Carryable>();

            if (item == null)
            {
                return null;
            }

            if (item == _releasedItem)
            {
                // Hängt sie bei uns noch an der Figur, hat der Host das Loslassen nur noch
                // nicht verarbeitet. Ein neues Aufheben kommt danach bei ihm an und klappt.
                bool free = !item.IsCarried || item.transform.parent == transform;
                return free ? item : null;
            }

            return !item.IsCarried && !item.IsLocallyHidden ? item : null;
        }

        // ----- Aufheb-Vorhersage -----

        private void StartGrabPrediction(Carryable item)
        {
            EndGrabPrediction();

            _pendingGrab = item;
            _grabStartTime = Time.time;
            _grabGhost = item.CreateLocalGhost(withPhysics: false);
            _grabGhost.transform.SetParent(transform, false);
            _grabGhost.transform.SetLocalPositionAndRotation(_holdLocalPosition, Quaternion.identity);
            item.SetLocallyHidden(true);
        }

        private void UpdateGrabPrediction()
        {
            if (_pendingGrab == null)
            {
                return;
            }

            // Bestätigt: Der Host hat die echte Kiste an uns gehängt und sie ist am Haltepunkt
            // angekommen (davor kann sie kurz vom Boden hochgleiten). Gleitet sie zu lange,
            // wird nach 0,4 s trotzdem getauscht, damit nichts hängen bleibt.
            bool attachedToMe = HeldItem == _pendingGrab;
            bool atHoldPoint = (_pendingGrab.transform.localPosition - _holdLocalPosition).sqrMagnitude < 0.01f;
            bool confirmed = attachedToMe && (atHoldPoint || Time.time - _grabStartTime > 0.4f);

            // Abgelehnt: jemand anderes trägt sie, oder der Host hat zu lange nicht geantwortet.
            bool takenByOther = _pendingGrab.IsCarried && _pendingGrab.transform.parent != transform;
            bool timedOut = !attachedToMe && Time.time - _grabStartTime > _grabTimeout;

            if (confirmed || takenByOther || timedOut)
            {
                EndGrabPrediction();
            }
        }

        private void EndGrabPrediction()
        {
            if (_grabGhost != null)
            {
                Destroy(_grabGhost);
            }

            if (_pendingGrab != null)
            {
                _pendingGrab.SetLocallyHidden(false);
            }

            _grabGhost = null;
            _pendingGrab = null;
        }

        // ----- Loslass-Vorhersage (Ablegen und Werfen) -----

        private void StartReleasePrediction(Carryable item, Vector3 velocity, Vector3 position, Quaternion rotation)
        {
            EndReleasePrediction();

            _releasedItem = item;
            _releaseStartTime = Time.time;
            _releaseGhost = item.CreateLocalGhost(withPhysics: true);
            _releaseGhost.transform.SetPositionAndRotation(position, rotation);
            _releaseGhostBody = _releaseGhost.GetComponent<Rigidbody>();
            _releaseGhostCollider = _releaseGhost.GetComponent<Collider>();
            _releaseGhostBody.linearVelocity = velocity;

            // Die Kopie soll weder die eigene Figur noch die (unsichtbare) echte Kiste treffen.
            if (_controller != null)
            {
                Physics.IgnoreCollision(_releaseGhostCollider, _controller, true);
            }

            item.SetLocallyHidden(true);
        }

        private void UpdateReleasePrediction()
        {
            if (_releaseGhost == null)
            {
                return;
            }

            Collider realCollider = _releasedItem.Collider;
            if (realCollider != null && realCollider.enabled)
            {
                // Wird beim Wieder-Einschalten des Colliders zurückgesetzt, deshalb jedes Frame.
                Physics.IgnoreCollision(_releaseGhostCollider, realCollider, true);
            }

            bool realReleased = !_releasedItem.IsCarried;
            bool takenByOther = _releasedItem.IsCarried && _releasedItem.transform.parent != transform;
            bool ghostResting = _releaseGhostBody.IsSleeping() || _releaseGhostBody.linearVelocity.sqrMagnitude < 0.01f;
            bool close = (_releasedItem.transform.position - _releaseGhost.transform.position).sqrMagnitude
                < _swapDistance * _swapDistance;
            bool timedOut = Time.time - _releaseStartTime > _releaseMaxTime;

            if ((realReleased && ghostResting && close) || takenByOther || timedOut)
            {
                EndReleasePrediction();
            }
        }

        private void EndReleasePrediction()
        {
            if (_releaseGhost != null)
            {
                Destroy(_releaseGhost);
            }

            if (_releasedItem != null)
            {
                _releasedItem.SetLocallyHidden(false);
            }

            _releaseGhost = null;
            _releaseGhostBody = null;
            _releaseGhostCollider = null;
            _releasedItem = null;
        }

        // ----- RPCs, laufen auf dem Host -----

        // Nur der Besitzer dieser Figur darf sie aufrufen.
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void PickUpRpc(NetworkObjectReference itemReference)
        {
            var heavyCarry = GetComponent<PlayerHeavyCarry>();
            if (HeldItem != null || (heavyCarry != null && heavyCarry.IsHolding))
            {
                return;
            }

            if (!itemReference.TryGet(out NetworkObject itemObject)
                || !itemObject.TryGetComponent(out Carryable item)
                || item.IsCarried)
            {
                return;
            }

            if (Vector3.Distance(item.transform.position, transform.position) > _maxServerDistance)
            {
                return;
            }

            item.AttachTo(NetworkObject, _holdLocalPosition);
        }

        // velocity = Vector3.zero heißt ablegen, sonst werfen.
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void DropRpc(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            Carryable item = HeldItem;
            if (item == null)
            {
                return;
            }

            // Unplausible Position: lieber die eigene Sicht des Hosts nehmen.
            if (Vector3.Distance(position, transform.position) > _maxServerDistance)
            {
                position = item.transform.position;
            }

            item.Detach(position, rotation, Vector3.ClampMagnitude(velocity, _maxThrowSpeed));
        }

        private void OnGUI()
        {
            if (!IsSpawned || !IsOwner)
            {
                return;
            }

            GUI.matrix = Matrix4x4.identity;
            _hintStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
            };

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            GUI.Label(new Rect(centerX - 20f, centerY - 15f, 40f, 30f), "+", _hintStyle);

            bool holding = CurrentlyHeld() != null || _pendingGrab != null;
            string hint = holding
                ? "E: ablegen     Linksklick: werfen"
                : _lookTarget != null ? "E: aufheben" : null;

            if (hint != null)
            {
                GUI.Label(new Rect(centerX - 200f, Screen.height - 70f, 400f, 30f), hint, _hintStyle);
            }
        }
    }
}
